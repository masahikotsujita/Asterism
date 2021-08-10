using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace Asterism {

    internal class ModuleGraph {
    
        public ModuleGraph(Context context, Module rootModule) {
            Context = context;
            RootModule = rootModule;
        }

        public Context Context { get; }
        
        public Module RootModule { get; }
        
        public IEnumerable<Module> SortedModules {
            get => TopologicalSort().Select(name => ModulesForNames[name]);
        }

        private Dictionary<string, Module> ModulesForNames { get; set; }

        public void LoadDependencies() {
            var modulesForNames = new Dictionary<string, Module>();
            void GetDependenciesInternal(Module module) {
                modulesForNames[module.Name] = module;
                if (module.Asterismfile.Dependencies != null) {
                    foreach (var dependency in module.Asterismfile.Dependencies) {
                        var submoduleName = GetModuleNameFromProject(dependency.Project);
                        if (!modulesForNames.ContainsKey(submoduleName)) {
                            var gitPath = $"https://github.com/{dependency.Project}.git";

                            var submoduleCheckoutPath = Path.Combine(Context.CheckoutDirectoryPath, submoduleName);

                            if (Directory.Exists(submoduleCheckoutPath)) {
                                var repository = new Repository(submoduleCheckoutPath);
                                var remote = repository.Network.Remotes["origin"];
                                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                                Commands.Fetch(repository, remote.Name, refSpecs, null, "");
                            } else {
                                Repository.Clone(gitPath, submoduleCheckoutPath);
                            }

                            var submodule = new Module(Context, submoduleName, submoduleCheckoutPath);
                            submodule.LoadAsterismfile();
                            GetDependenciesInternal(submodule);
                        }
                    }
                }
            }
            GetDependenciesInternal(RootModule);
            ModulesForNames = modulesForNames;
        }
        
        private Dictionary<string, HashSet<string>> GraphFromModuleForNames(Dictionary<string, Module> modulesForNames) {
            var graph = new Dictionary<string, HashSet<string>>();
            foreach (var moduleForName in modulesForNames) {
                graph.Add(moduleForName.Key, new HashSet<string>());
            }
            foreach (var moduleForName in modulesForNames) {
                if (moduleForName.Value.Asterismfile.Dependencies != null) {
                    foreach (var dependency in moduleForName.Value.Asterismfile.Dependencies) {
                        var moduleName = GetModuleNameFromProject(dependency.Project);
                        graph[moduleName].Add(moduleForName.Key);
                    }
                }
            }
            return graph;
        }

        private IEnumerable<string> TopologicalSort() {
            var graph = GraphFromModuleForNames(ModulesForNames);

            // Perform topological sort using Kahn's Algorithm
            // https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm
            var l = new List<string>();
            var s = graph.Where(nie => nie.Value.Count == 0)
                         .Select(nie => nie.Key)
                         .ToList();
            // Clone the graph to work
            var workGraph = new Dictionary<string, HashSet<string>>();
            foreach (var nie in graph) {
                var node = nie.Key;
                var incomingEdges = nie.Value;
                workGraph.Add(node, new HashSet<string>(incomingEdges));
            }

            while (s.Count > 0) {
                var n = s[0];
                s.RemoveAt(0);
                l.Add(n);
                foreach (var m in workGraph) {
                    if (m.Value.Contains(n)) {
                        m.Value.Remove(n);
                        if (m.Value.Count == 0) {
                            s.Add(m.Key);
                        }
                    }
                }
                workGraph.Remove(n);
            }

            l.Reverse();

            return workGraph.Count > 0 ? null : l;
        }

        public bool ResolveVersions() {
            var modules = SortedModules.ToList();
            var rangesForModuleNames = new Dictionary<string, List<Range>>();
            foreach (var module in modules) {
                rangesForModuleNames[module.Name] = new List<Range>();
            }
            foreach (var module in modules) {
                if (module.Asterismfile.Dependencies != null) {
                    foreach (var dependency in module.Asterismfile.Dependencies) {
                        var moduleName = GetModuleNameFromProject(dependency.Project);
                        var range = new Range(dependency.Version);
                        rangesForModuleNames[moduleName].Add(range);
                    }
                }
            }
            foreach (var module in modules) {
                if (module != RootModule) {
                    var selectedTag = module.Repository.Tags
                                            .Select<Tag, (Tag Tag, Version Version)>(tag => (tag, Version.TryParse(tag.FriendlyName, out var version) ? version : null))
                                            .Where(tuple => tuple.Version != null)
                                            .Where(tuple => {
                                                var success = true;
                                                foreach (var range in rangesForModuleNames[module.Name]) {
                                                    if (!range.IsSatisfied(tuple.Version)) {
                                                        success = false;
                                                    }
                                                }
                                                return success;
                                            })
                                            .OrderBy(tuple => tuple.Version)
                                            .Select(tuple => tuple.Tag)
                                            .LastOrDefault();
                    if (selectedTag != null) {
                        if (module.Repository.Head.Tip.Sha != selectedTag.Target.Sha) {
                            Commands.Checkout(module.Repository, selectedTag.CanonicalName);
                        }
                    } else {
                        return false;
                    }
                }
            }
            return true;
        }

        static string GetModuleNameFromProject(string project) {
            return project.Split('/')[1];
        }

    }

}

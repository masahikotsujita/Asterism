using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Caches = new Dictionary<string, MODULE_INFO> {
                [RootModule.Name] = new MODULE_INFO {
                    Module = rootModule,
                    IsFetched = true
                }
            };
        }

        public Context Context { get; }
        
        public Module RootModule { get; }

        struct MODULE_INFO {
            public Module Module { get; set; }
            public bool IsFetched { get; set; }
        }

        private Dictionary<string, MODULE_INFO> Caches { get; set; }
        
        public IEnumerable<Module> ResolveVersions() {
            while (true) {
                var moduleNames = GetDependenciesRecursively();
                var graph1 = new Dictionary<string, Module>();
                foreach (var moduleName in moduleNames) {
                    graph1[moduleName] = Caches[moduleName].Module;
                }
                var graph2 = GraphFromModuleForNames(graph1);
                var modules = TopologicalSort(graph2).Select(moduleName => Caches[moduleName].Module);
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
                bool allOk = true;
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
                                allOk = false;
                                break;
                            }
                        } else {
                            return null;
                        }
                    }
                }
                if (allOk) {
                    return modules;
                }
            }
        }

        private IEnumerable<string> GetDependenciesRecursively() {
            var result = new List<string> { RootModule.Name };
            void GetDependency(Asterismfile.DEPENDENCY dependency) {
                var moduleName = GetModuleNameFromProject(dependency.Project);
                result.Add(moduleName);
                if (!Caches.TryGetValue(moduleName, out var moduleInfo)) {
                    var moduleCheckoutPath = Path.Combine(Context.CheckoutDirectoryPath, moduleName);
                    if (!Directory.Exists(moduleCheckoutPath)) {
                        var githubPath = $"https://github.com/{dependency.Project}.git";
                        Repository.Clone(githubPath, moduleCheckoutPath);
                    }
                    moduleInfo = new MODULE_INFO {
                        Module = new Module(Context, moduleName, moduleCheckoutPath),
                        IsFetched = false
                    };
                    Caches[moduleName] = moduleInfo;
                }
                if (!moduleInfo.IsFetched) {
                    var repository = moduleInfo.Module.Repository;
                    var remote = repository.Network.Remotes["origin"];
                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repository, remote.Name, refSpecs, null, "");
                }
                moduleInfo.Module.LoadAsterismfile();
                if (moduleInfo.Module.Asterismfile.Dependencies != null) {
                    foreach (var innerDependency in moduleInfo.Module.Asterismfile.Dependencies) {
                        GetDependency(innerDependency);
                    }
                }
            }
            foreach (var dependency in RootModule.Asterismfile.Dependencies) {
                GetDependency(dependency);
            }
            return result;
        }
        
        private static Dictionary<string, HashSet<string>> GraphFromModuleForNames(Dictionary<string, Module> modulesForNames) {
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

        private static IEnumerable<string> TopologicalSort(Dictionary<string, HashSet<string>> graph) {

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

        static string GetModuleNameFromProject(string project) {
            return project.Split('/')[1];
        }

    }

}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace Asterism {

internal class ModuleManager {
    private struct ModuleInfo {
        public Module Module { get; set; }
        public bool IsFetched { get; set; }
    }

    private class ModuleGraph : Graph<string> {
        public ModuleGraph() {
            IncomingEdgesForNodes = new Dictionary<string, HashSet<string>>();
        }

        public Dictionary<string, HashSet<string>> IncomingEdgesForNodes { get; }
    }

    public ModuleManager(Context context, Module rootModule) {
        Context = context;
        RootModule = rootModule;
        Caches = new Dictionary<string, ModuleInfo> {
            [RootModule.Name] = new ModuleInfo {
                Module = rootModule,
                IsFetched = true
            }
        };
    }

    public IEnumerable<Module> ResolveVersions() {
        while (true) {
            var moduleNames = GetDependenciesRecursively();
            var graph1 = new Dictionary<string, Module>();
            foreach (var moduleName in moduleNames) {
                graph1[moduleName] = Caches[moduleName].Module;
            }
            var graph2 = GraphFromModuleForNames(graph1);
            var modules = graph2.TopologicalSort().Select(moduleName => Caches[moduleName].Module);
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
            var allOk = true;
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

        void GetDependency(DependencyInfo dependency) {
            var moduleName = GetModuleNameFromProject(dependency.Project);
            result.Add(moduleName);
            if (!Caches.TryGetValue(moduleName, out var moduleInfo)) {
                var moduleCheckoutPath = Path.Combine(Context.CheckoutDirectoryPath, moduleName);
                if (!Directory.Exists(moduleCheckoutPath)) {
                    var githubPath = $"https://github.com/{dependency.Project}.git";
                    Repository.Clone(githubPath, moduleCheckoutPath);
                }
                moduleInfo = new ModuleInfo {
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

    private static ModuleGraph GraphFromModuleForNames(Dictionary<string, Module> modulesForNames) {
        var graph = new ModuleGraph();
        foreach (var moduleForName in modulesForNames) {
            graph.IncomingEdgesForNodes.Add(moduleForName.Key, new HashSet<string>());
        }
        foreach (var moduleForName in modulesForNames) {
            if (moduleForName.Value.Asterismfile.Dependencies != null) {
                foreach (var dependency in moduleForName.Value.Asterismfile.Dependencies) {
                    var moduleName = GetModuleNameFromProject(dependency.Project);
                    graph.IncomingEdgesForNodes[moduleName].Add(moduleForName.Key);
                }
            }
        }
        return graph;
    }

    private static string GetModuleNameFromProject(string project) {
        return project.Split('/')[1];
    }

    public Context Context { get; }

    public Module RootModule { get; }

    private Dictionary<string, ModuleInfo> Caches { get; }
}

}
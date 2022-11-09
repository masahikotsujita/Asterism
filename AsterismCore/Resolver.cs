using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SemanticVersioning;
using YamlDotNet.Serialization;
using Version = SemanticVersioning.Version;
using YamlDotNet.Serialization.NamingConventions;

namespace AsterismCore {

public class Resolver {
    private class ModuleGraph : Graph<string> {
        public ModuleGraph() {
            IncomingEdgesForNodes = new Dictionary<string, HashSet<string>>();
        }

        public Dictionary<string, HashSet<string>> IncomingEdgesForNodes { get; }
    }

    public Resolver(Module rootModule) {
        RootModule = rootModule;
    }

    public void LoadLockFile()
    {
        if (!File.Exists(RootModule.Context.LockFilePath))
        {
            return;
        }
        var reader = File.OpenText(RootModule.Context.LockFilePath);
        var deserializer = new DeserializerBuilder()
                           .WithNamingConvention(UnderscoredNamingConvention.Instance)
                           .Build();
        LockDocument = deserializer.Deserialize<LockDocument>(reader);
        LockedRevisionsByModuleName = new Dictionary<string, string>();
        foreach (var dependency in LockDocument.Dependencies)
        {
            LockedRevisionsByModuleName[GetModuleNameFromProject(dependency.Project)] = dependency.Revision;
        }
    }

    public IEnumerable<Module> ResolveVersions() {
        while (true) {
            var moduleNames = GetDependenciesRecursively();
            var graph1 = new Dictionary<string, Module>();
            foreach (var moduleName in moduleNames) {
                graph1[moduleName] = RootModule.Context.Caches[moduleName];
            }
            var graph2 = GraphFromModuleForNames(graph1);
            var modules = graph2.TopologicalSort().Select(moduleName => RootModule.Context.Caches[moduleName]);
            var rangesForModuleNames = new Dictionary<string, List<Range>>();
            foreach (var module in modules) {
                rangesForModuleNames[module.Name] = new List<Range>();
            }
            foreach (var module in modules) {
                if (module.SpecDocument.Dependencies != null) {
                    foreach (var dependency in module.SpecDocument.Dependencies) {
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

    public IEnumerable<Module> ResolveVersionsUsingLockFile() {
        var moduleNames = GetDependenciesUsingLockFile();
        var modules = moduleNames.Select(moduleName => RootModule.Context.Caches[moduleName]);
        foreach (var module in modules) {
            if (module != RootModule) {
                var lockedRevisionSha1 = LockedRevisionsByModuleName[module.Name];
                if (module.Repository.Head.Tip.Sha != lockedRevisionSha1) {
                    Commands.Checkout(module.Repository, lockedRevisionSha1);
                    break;
                }
            }
        }
        return modules;
    }

    private IEnumerable<string> GetDependenciesRecursively() {
        var result = new List<string> { RootModule.Name };

        void GetDependency(DependencyInSpec dependency) {
            var moduleName = GetModuleNameFromProject(dependency.Project);
            result.Add(moduleName);
            if (!RootModule.Context.Caches.TryGetValue(moduleName, out var module)) {
                var moduleCheckoutPath = Path.Combine(RootModule.Context.CheckoutDirectoryPath, moduleName);
                if (!Directory.Exists(moduleCheckoutPath)) {
                    var githubPath = $"https://github.com/{dependency.Project}.git";
                    Repository.Clone(githubPath, moduleCheckoutPath);
                }
                module = new Module(RootModule.Context, moduleName, moduleCheckoutPath);
                module.IsFetched = false;
                module.ProjectPath = dependency.Project;
                RootModule.Context.Caches[moduleName] = module;
            }
            if (!module.IsFetched) {
                var repository = module.Repository;
                var remote = repository.Network.Remotes["origin"];
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(repository, remote.Name, refSpecs, null, "");
            }
            module.LoadSpecFile();
            if (module.SpecDocument.Dependencies != null) {
                foreach (var innerDependency in module.SpecDocument.Dependencies) {
                    GetDependency(innerDependency);
                }
            }
        }

        if (RootModule.SpecDocument.Dependencies != null) {
            foreach (var dependency in RootModule.SpecDocument.Dependencies) {
                GetDependency(dependency);
            }
        }
        return result;
    }

    private IEnumerable<string> GetDependenciesUsingLockFile() {
        var result = new List<string> { RootModule.Name };

        void GetDependency(DependencyInLock dependency) {
            var moduleName = GetModuleNameFromProject(dependency.Project);
            result.Add(moduleName);
            if (!RootModule.Context.Caches.TryGetValue(moduleName, out var module)) {
                var moduleCheckoutPath = Path.Combine(RootModule.Context.CheckoutDirectoryPath, moduleName);
                if (!Directory.Exists(moduleCheckoutPath)) {
                    var githubPath = $"https://github.com/{dependency.Project}.git";
                    Repository.Clone(githubPath, moduleCheckoutPath);
                }
                module = new Module(RootModule.Context, moduleName, moduleCheckoutPath);
                module.IsFetched = false;
                module.ProjectPath = dependency.Project;
                RootModule.Context.Caches[moduleName] = module;
            }
            if (!module.IsFetched) {
                var repository = module.Repository;
                var remote = repository.Network.Remotes["origin"];
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(repository, remote.Name, refSpecs, null, "");
            }
            module.LoadSpecFile();
        }

        foreach (var dependency in LockDocument.Dependencies) {
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
            if (moduleForName.Value.SpecDocument.Dependencies != null) {
                foreach (var dependency in moduleForName.Value.SpecDocument.Dependencies) {
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
    
    public Module RootModule { get; }

    private LockDocument LockDocument { get; set; }
    private Dictionary<string, string> LockedRevisionsByModuleName { get; set; }
}

}
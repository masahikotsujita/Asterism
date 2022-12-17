using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AsterismCore {

public class ModuleGraph : Graph<string> {
    public ModuleGraph() {
        IncomingEdgesForNodes = new Dictionary<string, HashSet<string>>();
    }

    public Dictionary<string, HashSet<string>> IncomingEdgesForNodes { get; }
}

public class Context {
    public Context(string workingDirectoryPath) {
        WorkingDirectoryPath = workingDirectoryPath;
        Caches = new Dictionary<string, Module>();
    }

    public string WorkingDirectoryPath { get; }

    public string AsterismDirectoryPath => Path.Combine(WorkingDirectoryPath, @".asterism\");

    public string LockFilePath => Path.Combine(WorkingDirectoryPath, @"asterismfile.lock");

    public string ArtifactsDirectoryPath => Path.Combine(AsterismDirectoryPath, @"artifacts\");

    public string CheckoutDirectoryPath => Path.Combine(AsterismDirectoryPath, @"checkout\");

    public Dictionary<string, Module> Caches { get; }

    public List<(Module module, VersionSpecifier versionSpecifier)> Dependencies { get; set; }

    public void SaveLockFile() {
        var lockDocument = new LockDocument {
            Dependencies = (from dependency in Dependencies
                            select new DependencyInLock() {
                                Project = dependency.module.ProjectPath,
                                Revision = dependency.module.GetSha1(dependency.versionSpecifier)
                            }).ToList()
        };
        var serializer = new SerializerBuilder()
                         .WithNamingConvention(UnderscoredNamingConvention.Instance)
                         .Build();
        var writer = new StreamWriter(LockFilePath, false);
        serializer.Serialize(writer, lockDocument);
        writer.Flush();
    }

    public ModuleGraph GetGraph(Module rootModule, Dictionary<string, VersionSpecifier> versionSpecifiersByModuleName) {
        var graph = new ModuleGraph {
            IncomingEdgesForNodes = {
                [rootModule.Key] = new HashSet<string>()
            }
        };
        void GetDependency(Module parentModule) {
            foreach (var (dependency, _) in parentModule.GetDependencies(versionSpecifiersByModuleName.TryGetValue(parentModule.Key, out var parentModuleVersionSpecifier) ? parentModuleVersionSpecifier : default)) {
                if (!graph.IncomingEdgesForNodes.TryGetValue(dependency.Key, out _)) {
                    graph.IncomingEdgesForNodes[dependency.Key] = new HashSet<string>();
                }
                graph.IncomingEdgesForNodes[dependency.Key].Add(parentModule.Key);
                GetDependency(dependency);
            }
        }
        GetDependency(rootModule);
        return graph;
    }
}

}
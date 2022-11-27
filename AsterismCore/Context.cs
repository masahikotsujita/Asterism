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
                [rootModule.Name] = new HashSet<string>()
            }
        };
        void GetDependency(Module parentModule) {
            foreach (var dependency in parentModule.GetRequirements(versionSpecifiersByModuleName.TryGetValue(parentModule.Name, out var parentModuleVersionSpecifier) ? parentModuleVersionSpecifier : VersionSpecifier.Default)) {
                if (!graph.IncomingEdgesForNodes.TryGetValue(dependency.Module.Name, out _)) {
                    graph.IncomingEdgesForNodes[dependency.Module.Name] = new HashSet<string>();
                }
                graph.IncomingEdgesForNodes[dependency.Module.Name].Add(parentModule.Name);
                GetDependency(dependency.Module);
            }
        }
        GetDependency(rootModule);
        return graph;
    }
}

}
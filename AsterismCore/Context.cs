using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AsterismCore {

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
}

}
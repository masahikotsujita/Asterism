using System.IO;

namespace AsterismCore {

public class Context {
    public Context(string workingDirectoryPath) {
        WorkingDirectoryPath = workingDirectoryPath;
    }

    public string WorkingDirectoryPath { get; }

    public string AsterismDirectoryPath => Path.Combine(WorkingDirectoryPath, @".asterism\");

    public string LockFilePath => Path.Combine(WorkingDirectoryPath, @"asterismfile.lock");

    public string ArtifactsDirectoryPath => Path.Combine(AsterismDirectoryPath, @"artifacts\");

    public string CheckoutDirectoryPath => Path.Combine(AsterismDirectoryPath, @"checkout\");
}

}
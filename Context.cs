using System.IO;

namespace Asterism {

internal class Context {
    public Context(string workingDirectoryPath) {
        WorkingDirectoryPath = workingDirectoryPath;
    }

    public string WorkingDirectoryPath { get; }

    public string AsterismDirectoryPath => Path.Combine(WorkingDirectoryPath, @".asterism\");

    public string ArtifactsDirectoryPath => Path.Combine(AsterismDirectoryPath, @"artifacts\");

    public string CheckoutDirectoryPath => Path.Combine(AsterismDirectoryPath, @"checkout\");
}

}
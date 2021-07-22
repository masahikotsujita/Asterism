using System.IO;

namespace Asterism {
    
    internal class Context {

        public Context(string workingDirectoryPath) {
            WorkingDirectoryPath = workingDirectoryPath;
        }

        public string WorkingDirectoryPath { get; }

        public string AsterismDirectoryPath {
            get { return Path.Combine(WorkingDirectoryPath, @".asterism\"); }
        }

        public string ArtifactsDirectoryPath {
            get { return Path.Combine(AsterismDirectoryPath, @"artifacts\"); }
        }

        public string CheckoutDirectoryPath {
            get { return Path.Combine(AsterismDirectoryPath, @"checkout\"); }
        }

    }
    
}
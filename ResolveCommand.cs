using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Asterism {

    class ResolveCommand {
        
        public ResolveCommand(ResolveOptions options) {
            this.Options = options;
        }

        public void Run() {
            var workingDirectoryPath = Directory.GetCurrentDirectory();
            var asterismfilePath = Path.Combine(workingDirectoryPath, ".asterismfile.yml");
            var asterismfile = new Asterismfile(asterismfilePath);
            var asterismDirPath = Path.Combine(workingDirectoryPath, ".asterism");
            var checkoutDir = Path.Combine(asterismDirPath, "checkout");
            foreach (String dependency in asterismfile.Dependencies) {
                var moduleGitURL = $"https://github.com/{dependency}.git";
                var moduleName = dependency.Split('/')[1];
                var moduleCheckoutPath = Path.Combine(checkoutDir, moduleName);
                if (!Directory.Exists(moduleCheckoutPath)) {
                    Repository.Clone(moduleGitURL, moduleCheckoutPath);
                }
                var moduleAsterismfilePath = Path.Combine(moduleCheckoutPath, ".asterismfile.yml");
                var moduleAsterismfile = new Asterismfile(moduleAsterismfilePath);
                var moduleSolutionFilePath = Path.Combine(moduleCheckoutPath, moduleAsterismfile.SolutionFilePath.Replace('/', '\\'));
                MSBuild.Build(moduleSolutionFilePath, (message) => {
                    Console.WriteLine(message);
                });
            }
        }

        public ResolveOptions Options { get; }

    }

}

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

        public int Run() {
            var workingDirectoryPath = Directory.GetCurrentDirectory();
            var asterismfilePath = Path.Combine(workingDirectoryPath, ".asterismfile.yml");
            var asterismfile = new Asterismfile(asterismfilePath);
            var asterismDirPath = Path.Combine(workingDirectoryPath, ".asterism");
            var checkoutDirPath = Path.Combine(asterismDirPath, @"checkout\");
            var artifactsDirPath = Path.Combine(asterismDirPath, @"artifacts\");

            var libraries = new List<String>();

            foreach (String dependency in asterismfile.Dependencies) {

                var moduleGitURL = $"https://github.com/{dependency}.git";
                var moduleName = dependency.Split('/')[1];
                var moduleCheckoutPath = Path.Combine(checkoutDirPath, moduleName);
                if (!Directory.Exists(moduleCheckoutPath)) {
                    Repository.Clone(moduleGitURL, moduleCheckoutPath);
                }

                var moduleAsterismfilePath = Path.Combine(moduleCheckoutPath, ".asterismfile.yml");
                var moduleAsterismfile = new Asterismfile(moduleAsterismfilePath);

                var moduleSolutionFilePath = Path.Combine(moduleCheckoutPath, FileUtility.ReplacePathSeparatorsForWindows(moduleAsterismfile.SolutionFilePath));
                var relativePathFromModuleSolutionDirToArtifactsDir = FileUtility.GetRelativePath(moduleSolutionFilePath, artifactsDirPath);

                var moduleAsterismDirPath = Path.Combine(moduleCheckoutPath, @".asterism\");
                var moduleAsterismPropsFilePath = Path.Combine(moduleAsterismDirPath, @"vsprops\", "Asterism.props");

                var moduleProps = new PropertySheet();
                moduleProps.UserMacros.Add(new KeyValuePair<string, string>("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromModuleSolutionDirToArtifactsDir}"));
                moduleProps.AdditionalIncludeDirectories = $"$(AsterismArtifactsDir)x64\\Debug\\include";
                moduleProps.Save(moduleAsterismPropsFilePath);

                var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(moduleSolutionFilePath);
                var buildExitCode = MsBuildUtility.Build(moduleSolutionFilePath, (message) => {
                    Console.WriteLine(message);
                });
                if (buildExitCode != 0) {
                    return buildExitCode;
                }
                
                if (moduleAsterismfile.Artifacts is Asterismfile.ARTIFACTS artifacts) {
                    var headerDestination = Path.Combine(artifactsDirPath, @"x64\Debug\include\");
                    foreach (var headerPattern in artifacts.IncludeHeaders) {
                        var headerSource = FileUtility.ReplacePathSeparatorsForWindows(headerPattern);
                        var xcopyExitCode = FileUtility.XCopy(headerSource, headerDestination, moduleCheckoutPath, (message) => {
                            Console.WriteLine(message);
                        });
                        if (xcopyExitCode != 0) {
                            return xcopyExitCode;
                        }
                    }
                    var libDestination = Path.Combine(artifactsDirPath, @"x64\Debug\lib\");
                    foreach (var libraryPattern in artifacts.LinkLibraries) {
                        var libSource = FileUtility.ReplacePathSeparatorsForWindows(libraryPattern).Replace("${PLATFORM}", "x64").Replace("${CONFIGURATION}", "Debug");
                        var lib = Path.GetFileName(libSource);
                        var xcopyExitCode = FileUtility.XCopy(libSource, libDestination, moduleCheckoutPath, (message) => {
                            Console.WriteLine(message);
                        });
                        if (xcopyExitCode != 0) {
                            return xcopyExitCode;
                        }
                        libraries.Add(lib);
                    }
                }
            }

            var solutionFilePath = Path.Combine(workingDirectoryPath, FileUtility.ReplacePathSeparatorsForWindows(asterismfile.SolutionFilePath));
            var relativePathFromSolutionDirToArtifactsDir = FileUtility.GetRelativePath(solutionFilePath, artifactsDirPath);

            var additionalDependencies = String.Join(";", libraries.ToArray());

            var rootProps = new PropertySheet();
            rootProps.UserMacros.Add(new KeyValuePair<string, string>("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromSolutionDirToArtifactsDir}"));
            rootProps.AdditionalDependencies = additionalDependencies;
            rootProps.AdditionalLibraryDirectories = @"$(AsterismArtifactsDir)x64\Debug\lib\";
            rootProps.AdditionalIncludeDirectories = @"$(AsterismArtifactsDir)x64\Debug\include\";
            var propsFilePath = Path.Combine(asterismDirPath, "vsprops", "Asterism.props");
            rootProps.Save(propsFilePath);

            return 0;
        }
        
        public ResolveOptions Options { get; }

    }

}

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

            var solutionFilePath = Path.Combine(workingDirectoryPath, FileUtility.ReplacePathSeparatorsForWindows(asterismfile.SolutionFilePath));
            var rootSolutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solutionFilePath);
            var configurations = rootSolutionFile.SolutionConfigurations;

            var librariesForConfigurations = new Dictionary<String, List<String>>();
            foreach (var configuration in configurations) {
                librariesForConfigurations[configuration.FullName] = new List<String>();
            }

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
                moduleProps.Configurations.AddRange(from configuration in configurations select configuration.FullName);
                moduleProps.UserMacros.Add(new KeyValuePair<string, string>("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromModuleSolutionDirToArtifactsDir}"));
                foreach (var configuration in configurations) {
                    moduleProps.AdditionalIncludeDirectories[configuration.FullName] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include";
                }
                moduleProps.Save(moduleAsterismPropsFilePath);

                var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(moduleSolutionFilePath);
                foreach (var configuration in configurations) {
                    var buildExitCode = MsBuildUtility.Build(moduleSolutionFilePath, new Dictionary<String, String> { { "Platform", configuration.PlatformName }, { "Configuration", configuration.ConfigurationName } }, (message) => {
                        Console.WriteLine(message);
                    });
                    if (buildExitCode != 0) {
                        return buildExitCode;
                    }
                }
                
                if (moduleAsterismfile.Artifacts is Asterismfile.ARTIFACTS artifacts) {
                    foreach (var configuration in configurations) {
                        var headerDestination = Path.Combine(artifactsDirPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\");
                        foreach (var headerPattern in artifacts.IncludeHeaders) {
                            var headerSource = FileUtility.ReplacePathSeparatorsForWindows(headerPattern);
                            var xcopyExitCode = FileUtility.XCopy(headerSource, headerDestination, moduleCheckoutPath, (message) => {
                                Console.WriteLine(message);
                            });
                            if (xcopyExitCode != 0) {
                                return xcopyExitCode;
                            }
                        }
                        var libDestination = Path.Combine(artifactsDirPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\");
                        foreach (var libraryPattern in artifacts.LinkLibraries) {
                            var libSource = FileUtility.ReplacePathSeparatorsForWindows(libraryPattern).Replace("${PLATFORM}", configuration.PlatformName).Replace("${CONFIGURATION}", configuration.ConfigurationName);
                            var lib = Path.GetFileName(libSource);
                            var xcopyExitCode = FileUtility.XCopy(libSource, libDestination, moduleCheckoutPath, (message) => {
                                Console.WriteLine(message);
                            });
                            if (xcopyExitCode != 0) {
                                return xcopyExitCode;
                            }
                            librariesForConfigurations[configuration.FullName].Add(lib);
                        }
                    }
                }
            }

            var relativePathFromSolutionDirToArtifactsDir = FileUtility.GetRelativePath(solutionFilePath, artifactsDirPath);
            var rootProps = new PropertySheet();
            rootProps.Configurations.AddRange(from configuration in configurations select configuration.FullName);
            rootProps.UserMacros.Add(new KeyValuePair<string, string>("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromSolutionDirToArtifactsDir}"));
            foreach (var configuration in configurations) {
                var additionalDependencies = String.Join(";", librariesForConfigurations[configuration.FullName].ToArray());
                rootProps.AdditionalDependencies[configuration.FullName] = additionalDependencies;
                rootProps.AdditionalLibraryDirectories[configuration.FullName] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\";
                rootProps.AdditionalIncludeDirectories[configuration.FullName] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\";
            }
            var propsFilePath = Path.Combine(asterismDirPath, "vsprops", "Asterism.props");
            rootProps.Save(propsFilePath);

            return 0;
        }
        
        public ResolveOptions Options { get; }

    }

}

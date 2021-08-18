using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Build.Construction;

namespace Asterism {
    
    internal class Module {
    
        public Module(Context context, string name, string checkoutDirectoryPath) {
            Context = context;
            Name = name;
            CheckoutDirectoryPath = checkoutDirectoryPath;
            Repository = new Repository(CheckoutDirectoryPath);
        }

        public Context Context { get; }

        public string Name { get; }
    
        public string CheckoutDirectoryPath { get; }

        public Repository Repository { get; }
    
        public string AsterismDirectoryPath {
            get { return Path.Combine(CheckoutDirectoryPath, ".asterism\\"); }
        }
    
        public string AsterismfilePath {
            get { return Path.Combine(CheckoutDirectoryPath, ".asterismfile.yml"); }
        }
    
        public Asterismfile Asterismfile { get; private set; }
    
        public string SolutionFilePath { get; private set; }
    
        public SolutionFile SolutionFile { get; private set; }
    
        public void LoadAsterismfile() {
            Asterismfile = new Asterismfile(AsterismfilePath);
            SolutionFilePath = Path.Combine(CheckoutDirectoryPath,
                FileUtility.ReplacePathSeparatorsForWindows(Asterismfile.SolutionFilePath));
        }
    
        public void LoadSolutionFile() {
            SolutionFile = SolutionFile.Parse(SolutionFilePath);
        }
    
        public void CreatePropertySheet(bool forApplication, Dictionary<string, List<string>> librariesForConfigurations) {
            var configurations = SolutionFile.SolutionConfigurations;
            string relativePathFromSlnToArtifactsDir = FileUtility.GetRelativePath(SolutionFilePath, Context.ArtifactsDirectoryPath);
            string propertySheetPath = Path.Combine(AsterismDirectoryPath, "vsprops\\Asterism.props");
            var propertySheet = new PropertySheet();
            propertySheet.Configurations.AddRange(from configuration in configurations
                                                  select configuration.FullName);
            propertySheet.UserMacros.Add(new KeyValuePair<string, string>("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromSlnToArtifactsDir}"));
            foreach (var configuration in configurations) {
                propertySheet.AdditionalIncludeDirectories[configuration.FullName] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include";
            }
    
            if (forApplication) {
                foreach (var configuration in configurations) {
                    string additionalDependencies = string.Join(";", librariesForConfigurations[configuration.FullName].ToArray());
                    propertySheet.AdditionalDependencies[configuration.FullName] = additionalDependencies;
                    propertySheet.AdditionalLibraryDirectories[configuration.FullName] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\";
                    propertySheet.AdditionalIncludeDirectories[configuration.FullName] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\";
                }
            }
    
            propertySheet.Save(propertySheetPath);
        }
    
        public bool Build(SolutionConfigurationInSolution configuration, ref List<string> librariesToBeLinked) {
            int buildExitCode = MsBuildUtility.Build(SolutionFilePath, new Dictionary<string, string> { { "Platform", configuration.PlatformName }, { "Configuration", configuration.ConfigurationName } }, message => { Console.WriteLine(message); });
            if (buildExitCode != 0) {
                return false;
            }
    
            if (Asterismfile.ArtifactsInfo is ArtifactsInfo artifacts) {
                string headerDestination = Path.Combine(Context.ArtifactsDirectoryPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\");
                foreach (string headerPattern in artifacts.IncludeHeaders) {
                    string headerSource = FileUtility.ReplacePathSeparatorsForWindows(headerPattern);
                    int xcopyExitCode = FileUtility.XCopy(headerSource, headerDestination, CheckoutDirectoryPath, message => { Console.WriteLine(message); });
                    if (xcopyExitCode != 0) {
                        return false;
                    }
                }
    
                string libDestination = Path.Combine(Context.ArtifactsDirectoryPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\");
                foreach (string libraryPattern in artifacts.LinkLibraries) {
                    string libSource = FileUtility.ReplacePathSeparatorsForWindows(libraryPattern).Replace("${PLATFORM}", configuration.PlatformName).Replace("${CONFIGURATION}", configuration.ConfigurationName);
                    string lib = Path.GetFileName(libSource);
                    int xcopyExitCode = FileUtility.XCopy(libSource, libDestination, CheckoutDirectoryPath, message => { Console.WriteLine(message); });
                    if (xcopyExitCode != 0) {
                        return false;
                    }

                    librariesToBeLinked.Add(lib);
                }
            }
    
            return true;
        }

    }

}
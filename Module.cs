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

    public void LoadAsterismfile() {
        Asterismfile = new Asterismfile(AsterismfilePath);
        SolutionFilePath = Path.Combine(CheckoutDirectoryPath,
            FileUtility.ReplacePathSeparatorsForWindows(Asterismfile.SolutionFilePath));
    }

    public void LoadSolutionFile() {
        SolutionFile = SolutionFile.Parse(SolutionFilePath);
    }

    public void CreatePropertySheet(bool forApplication, Dictionary<BuildConfiguration, List<string>> librariesForConfigurations) {
        var configurations = SolutionFile.SolutionConfigurations
                                         .Select(x => new BuildConfiguration(x));
        var relativePathFromSlnToArtifactsDir = FileUtility.GetRelativePath(SolutionFilePath, Context.ArtifactsDirectoryPath);
        var propertySheetPath = Path.Combine(AsterismDirectoryPath, "vsprops\\Asterism.props");
        var propertySheet = new PropertySheet();
        propertySheet.Configurations.AddRange(from configuration in configurations
                                              select configuration);
        propertySheet.UserMacros.Add(new KeyValuePair<string, string>("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromSlnToArtifactsDir}"));
        foreach (var configuration in configurations) {
            propertySheet.AdditionalIncludeDirectories[configuration] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include";
        }

        if (forApplication) {
            foreach (var configuration in configurations) {
                var additionalDependencies = string.Join(";", librariesForConfigurations[configuration].ToArray());
                propertySheet.AdditionalDependencies[configuration] = additionalDependencies;
                propertySheet.AdditionalLibraryDirectories[configuration] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\";
                propertySheet.AdditionalIncludeDirectories[configuration] = $"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\";
            }
        }

        propertySheet.Save(propertySheetPath);
    }

    public bool Build(BuildConfiguration configuration, ref List<string> librariesToBeLinked) {
        var buildExitCode = MsBuildUtility.Build(SolutionFilePath, new Dictionary<string, string> { { "Platform", configuration.PlatformName }, { "Configuration", configuration.ConfigurationName } }, message => { Console.WriteLine(message); });
        if (buildExitCode != 0) {
            return false;
        }

        if (Asterismfile.ArtifactsInfo is ArtifactsInfo artifacts) {
            var headerDestination = Path.Combine(Context.ArtifactsDirectoryPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\");
            foreach (var headerPattern in artifacts.IncludeHeaders) {
                var headerSource = FileUtility.ReplacePathSeparatorsForWindows(headerPattern);
                var xcopyExitCode = FileUtility.XCopy(headerSource, headerDestination, CheckoutDirectoryPath, message => { Console.WriteLine(message); });
                if (xcopyExitCode != 0) {
                    return false;
                }
            }

            var libDestination = Path.Combine(Context.ArtifactsDirectoryPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\");
            foreach (var libraryPattern in artifacts.LinkLibraries) {
                var libSource = FileUtility.ReplacePathSeparatorsForWindows(libraryPattern).Replace("${PLATFORM}", configuration.PlatformName).Replace("${CONFIGURATION}", configuration.ConfigurationName);
                var lib = Path.GetFileName(libSource);
                var xcopyExitCode = FileUtility.XCopy(libSource, libDestination, CheckoutDirectoryPath, message => { Console.WriteLine(message); });
                if (xcopyExitCode != 0) {
                    return false;
                }

                librariesToBeLinked.Add(lib);
            }
        }

        return true;
    }

    public Context Context { get; }

    public string Name { get; }

    public string CheckoutDirectoryPath { get; }

    public Repository Repository { get; }

    public string AsterismDirectoryPath => Path.Combine(CheckoutDirectoryPath, ".asterism\\");

    public string AsterismfilePath => Path.Combine(CheckoutDirectoryPath, ".asterismfile.yml");

    public Asterismfile Asterismfile { get; private set; }

    public string SolutionFilePath { get; private set; }

    public SolutionFile SolutionFile { get; private set; }
}

}
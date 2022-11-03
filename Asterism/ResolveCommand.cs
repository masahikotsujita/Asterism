using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsterismCore;

namespace Asterism {

internal class ResolveCommand {
    public ResolveCommand(ResolveOptions options) {
        Options = options;
    }

    public int Run() {
        var workingDirectoryPath = Directory.GetCurrentDirectory();

        var context = new Context(workingDirectoryPath);

        var rootModuleName = Path.GetFileName(Path.GetFullPath(workingDirectoryPath));
        var rootModule = new Module(context, rootModuleName, workingDirectoryPath);
        rootModule.LoadSpecFile();

        var graph = new ModuleManager(context, rootModule);
        var allModules = graph.ResolveVersions();
        if (allModules == null) {
            return 1;
        }
        var modules = allModules.Where(x => x != rootModule);

        rootModule.LoadSolutionFile();
        var configurations = rootModule.SolutionFile.SolutionConfigurations
                                       .Select(x => new BuildConfiguration(x))
                                       .Where(ShouldBuildPlatformConfiguration);
        var librariesForConfigurations = new Dictionary<BuildConfiguration, List<string>>();
        foreach (var configuration in configurations) {
            librariesForConfigurations[configuration] = new List<string>();
        }

        foreach (var module in modules) {
            module.LoadSolutionFile();
            module.CreatePropertySheet(false, null);
            foreach (var configuration in configurations) {
                var libraries = librariesForConfigurations[configuration];
                if (!module.Build(configuration, ref libraries)) {
                    return 1;
                }
            }
        }

        rootModule.CreatePropertySheet(true, librariesForConfigurations);

        return 0;
    }

    private bool ShouldBuildPlatformConfiguration(BuildConfiguration configuration) {
        bool ShouldBuildPlatform(string platformName) {
            if (!Options.Platforms.Any()) {
                return true;
            }
            return Options.Platforms.Contains(platformName);
        }

        bool ShouldBuildConfiguration(string configurationName) {
            if (!Options.Configurations.Any()) {
                return true;
            }
            return Options.Configurations.Contains(configurationName);
        }

        return ShouldBuildPlatform(configuration.PlatformName) && ShouldBuildConfiguration(configuration.ConfigurationName);
    }

    public ResolveOptions Options { get; }
}

}
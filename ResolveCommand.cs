using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        rootModule.LoadAsterismfile();

        var graph = new ModuleGraph(context, rootModule);
        var allModules = graph.ResolveVersions();
        if (allModules == null) {
            return 1;
        }
        var modules = allModules.Where(x => x != rootModule);

        rootModule.LoadSolutionFile();
        var configurations = from c in rootModule.SolutionFile.SolutionConfigurations
                             where ShouldBuildPlatformConfiguration(c.PlatformName, c.ConfigurationName)
                             select c;
        var librariesForConfigurations = new Dictionary<string, List<string>>();
        foreach (var configuration in configurations) {
            librariesForConfigurations[configuration.FullName] = new List<string>();
        }

        foreach (var module in modules) {
            module.LoadSolutionFile();
            module.CreatePropertySheet(false, null);
            foreach (var configuration in configurations) {
                var libraries = librariesForConfigurations[configuration.FullName];
                if (!module.Build(configuration, ref libraries)) {
                    return 1;
                }
            }
        }

        rootModule.CreatePropertySheet(true, librariesForConfigurations);

        return 0;
    }

    private bool ShouldBuildPlatform(string platformName) {
        if (!Options.Platforms.Any()) {
            return true;
        }
        return Options.Platforms.Contains(platformName);
    }

    private bool ShouldBuildConfiguration(string configurationName) {
        if (!Options.Configurations.Any()) {
            return true;
        }
        return Options.Configurations.Contains(configurationName);
    }

    private bool ShouldBuildPlatformConfiguration(string platformName, string configurationName) {
        return ShouldBuildPlatform(platformName) && ShouldBuildConfiguration(configurationName);
    }

    public ResolveOptions Options { get; }
}

}
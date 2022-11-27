using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsterismCore;

namespace Asterism {

internal class InitCommand {
    internal InitCommand(InitOptions options) {
        Options = options;
    }

    public int Run() {
        var workingDirectoryPath = Directory.GetCurrentDirectory();

        var context = new Context(workingDirectoryPath);

        var rootModuleName = Path.GetFileName(Path.GetFullPath(workingDirectoryPath));
        var rootModule = new Module(context, rootModuleName, true);
        context.Caches[rootModule.Name] = rootModule;

        var resolver = new Resolver(rootModule);

        var resolvedVersionSpecifiersByModuleName = resolver.ResolveVersions();
        var pinnedDependencies = rootModule.GetRequirements(VersionSpecifier.Default);
        var moduleAndVersionSpecifiers = pinnedDependencies
            .Select(requirement => (module: requirement.Module, versionSpecifier: resolvedVersionSpecifiersByModuleName[requirement.Module.Name]));
        
        rootModule.LoadSolutionFile();
        var configurations = rootModule.SolutionFile.SolutionConfigurations
                                       .Select(x => new BuildConfiguration(x))
                                       .Where(ShouldBuildPlatformConfiguration);
        var librariesForConfigurations = new Dictionary<BuildConfiguration, List<string>>();
        foreach (var configuration in configurations) {
            librariesForConfigurations[configuration] = new List<string>();
        }

        foreach (var moduleAndVersionSpecifier in moduleAndVersionSpecifiers) {
            moduleAndVersionSpecifier.module.EnsureCheckout(moduleAndVersionSpecifier.versionSpecifier);
            moduleAndVersionSpecifier.module.LoadSolutionFile();
            moduleAndVersionSpecifier.module.CreatePropertySheet(false, null);
            foreach (var configuration in configurations) {
                var libraries = librariesForConfigurations[configuration];
                if (!moduleAndVersionSpecifier.module.Build(configuration, ref libraries)) {
                    return 1;
                }
            }
        }

        rootModule.CreatePropertySheet(true, librariesForConfigurations);

        context.Dependencies = moduleAndVersionSpecifiers.ToList();
        context.SaveLockFile();

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

    public InitOptions Options { get; }
}

}
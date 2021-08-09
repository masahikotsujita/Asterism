using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Asterism {

    class ResolveCommand {
        
        public ResolveCommand(ResolveOptions options) {
            this.Options = options;
        }

        public int Run() {
            var workingDirectoryPath = Directory.GetCurrentDirectory();

            var context = new Context(workingDirectoryPath);

            var rootModuleName = Path.GetFileName(Path.GetFullPath(workingDirectoryPath));
            var rootModule = new Module(context, rootModuleName, workingDirectoryPath);
            rootModule.LoadAsterismfile();

            var graph = new ModuleGraph(context, rootModule);
            graph.LoadDependencies();
            if (!graph.ResolveVersions()) {
                return 1;
            }
            var modules = from module in graph.SortedModules
                          where module != rootModule
                          select module;

            rootModule.LoadSolutionFile();
            var configurations = from c in rootModule.SolutionFile.SolutionConfigurations
                                 where ShouldBuildPlatformConfiguration(c.PlatformName, c.ConfigurationName)
                                 select c;
            var librariesForConfigurations = new Dictionary<String, List<String>>();
            foreach (var configuration in configurations) {
                librariesForConfigurations[configuration.FullName] = new List<String>();
            }

            foreach (var module in modules) {
                module.LoadSolutionFile();
                module.CreatePropertySheet(false, null);
                foreach (var configuration in configurations) {
                    List<String> libraries = librariesForConfigurations[configuration.FullName];
                    if (!module.Build(configuration, ref libraries)) {
                        return 1;
                    }
                }
            }
            
            rootModule.CreatePropertySheet(true, librariesForConfigurations);

            return 0;
        }
        
        public ResolveOptions Options { get; }

        bool ShouldBuildPlatform(String platformName) {
            if (!Options.Platforms.Any()) {
                return true;
            }
            return Options.Platforms.Contains(platformName);
        }

        bool ShouldBuildConfiguration(String configurationName) {
            if (!Options.Configurations.Any()) {
                return true;
            }
            return Options.Configurations.Contains(configurationName);
        }

        bool ShouldBuildPlatformConfiguration(String platformName, String configurationName) {
            return ShouldBuildPlatform(platformName) && ShouldBuildConfiguration(configurationName);
        }

    }

}

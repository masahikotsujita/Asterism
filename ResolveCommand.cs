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

            var context = new Context(workingDirectoryPath);

            var modules = new List<Module>();

            var rootModule = new Module(context, workingDirectoryPath);
            rootModule.LoadAsterismfile();
            
            foreach (String dependency in rootModule.Asterismfile.Dependencies) {

                var moduleName = dependency.Split('/')[1];

                var gitPath = $"https://github.com/{dependency}.git";

                var moduleCheckoutPath = Path.Combine(context.CheckoutDirectoryPath, moduleName);
                
                var module = Directory.Exists(moduleCheckoutPath) ?
                    new Module(context, moduleCheckoutPath) :
                    Module.Clone(context, gitPath, moduleCheckoutPath);
                module.LoadAsterismfile();
                
                modules.Add(module);
            }

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

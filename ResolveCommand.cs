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

            var rootModuleName = Path.GetFileName(Path.GetFullPath(workingDirectoryPath));
            var rootModule = new Module(context, rootModuleName, workingDirectoryPath);
            rootModule.LoadAsterismfile();
            
            var modulesForNames = GetModulesForNames(context, rootModule);

            var graph = GraphFromModuleForNames(modulesForNames);

            var modules = from moduleName in TopologicalSort(graph)
                          where moduleName != rootModuleName
                          select modulesForNames[moduleName];

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

        Dictionary<string, Module> GetModulesForNames(Context context, Module rootModule) {
            var modulesForNames = new Dictionary<string, Module>();
            void GetDependencies(Module module) {
                modulesForNames[module.Name] = module;
                if (module.Asterismfile.Dependencies != null) {
                    foreach (var dependency in module.Asterismfile.Dependencies) {
                        var submoduleName = dependency.Split('/')[1];
                        if (!modulesForNames.ContainsKey(submoduleName)) {
                            var gitPath = $"https://github.com/{dependency}.git";

                            var submoduleCheckoutPath = Path.Combine(context.CheckoutDirectoryPath, submoduleName);

                            var submodule = Directory.Exists(submoduleCheckoutPath) ?
                                new Module(context, submoduleName, submoduleCheckoutPath) :
                                Module.Clone(context, submoduleName, gitPath, submoduleCheckoutPath);
                            submodule.LoadAsterismfile();
                            GetDependencies(submodule);
                        }
                    }
                }
            }
            GetDependencies(rootModule);
            return modulesForNames;
        }

        Dictionary<string, HashSet<string>> GraphFromModuleForNames(Dictionary<string, Module> modulesForNames) {
            var graph = new Dictionary<string, HashSet<string>>();
            foreach (var moduleForName in modulesForNames) {
                graph.Add(moduleForName.Key, new HashSet<string>());
            }
            foreach (var moduleForName in modulesForNames) {
                if (moduleForName.Value.Asterismfile.Dependencies != null) {
                    foreach (var dependency in moduleForName.Value.Asterismfile.Dependencies) {
                        var moduleName = dependency.Split('/')[1];
                        graph[moduleName].Add(moduleForName.Key);
                    }
                }
            }
            return graph;
        }

        private static IEnumerable<string> TopologicalSort(Dictionary<string, HashSet<string>> graph) {
            // Perform topological sort using Kahn's Algorithm
            // https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm
            var l = new List<string>();
            var s = graph.Where(nie => nie.Value.Count == 0)
                         .Select(nie => nie.Key)
                         .ToList();
            // Clone the graph to work
            var workGraph = new Dictionary<string, HashSet<string>>();
            foreach (var nie in graph) {
                var node = nie.Key;
                var incomingEdges = nie.Value;
                workGraph.Add(node, new HashSet<string>(incomingEdges));
            }

            while (s.Count > 0) {
                var n = s[0];
                s.RemoveAt(0);
                l.Add(n);
                foreach (var m in workGraph) {
                    if (m.Value.Contains(n)) {
                        m.Value.Remove(n);
                        if (m.Value.Count == 0) {
                            s.Add(m.Key);
                        }
                    }
                }
                workGraph.Remove(n);
            }

            l.Reverse();

            return workGraph.Count > 0 ? null : l;
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

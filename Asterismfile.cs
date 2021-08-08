using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using YamlDotNet;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Asterism {

    class Asterismfile {

        public struct DEPENDENCY {
            public string Project { get; set; }
            public string Version { get; set; }
        }

        public struct ARTIFACTS
        {
            public IEnumerable<String> IncludeHeaders { get; set; }
            public IEnumerable<String> LinkLibraries { get; set; }
        }

        public Asterismfile(String filePath) {
            var reader = new StreamReader(filePath);
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);
            var yaml = new YAML(yamlStream.Documents[0].RootNode);

            this.Name = yaml["name"].String;

            this.Dependencies = yaml["dependencies"]
                .List
                ?.Select(yml => new DEPENDENCY {
                    Project = yml["project"].String,
                    Version = yml["version"].String
                });

            this.SolutionFilePath = yaml["sln_path"].String;

            var includeHeaders = yaml["artifacts"]["include_headers"].List?.Select(yml => yml.String);
            var linkLibraries = yaml["artifacts"]["link_libraries"].List?.Select(yml => yml.String);
            if (includeHeaders != null || linkLibraries != null) {
                this.Artifacts = new ARTIFACTS {
                    IncludeHeaders = includeHeaders,
                    LinkLibraries = linkLibraries
                };
            }
        }

        public String Name { get; }

        public IEnumerable<DEPENDENCY> Dependencies { get; }

        public String SolutionFilePath { get; }

        public ARTIFACTS? Artifacts { get; }

    }

}

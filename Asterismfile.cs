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

        public struct Artifact
        {
            public String Source { get; set; }
            public String Destination { get; set; }
        }

        public Asterismfile(String filePath) {
            var reader = new StreamReader(filePath);
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);
            var yaml = new Yaml(yamlStream.Documents[0].RootNode);

            this.Name = yaml["name"].String;

            this.Dependencies = yaml["dependencies"]
                .List
                ?.Select(yml => yml.String);

            this.SolutionFilePath = yaml["sln_path"].String;

            this.Artifacts = yaml["artifacts"]
                .List
                ?.Select(yml => {
                    String source, destination;
                    if ((source = yml["src"].String) != null && (destination = yml["dst"].String) != null) {
                        return new Artifact {
                            Source = source,
                            Destination = destination
                        } as Artifact?;
                    } else {
                        return null;
                    }
                })
                .OfType<Artifact>();
        }

        public String Name { get; }

        public IEnumerable<String> Dependencies { get; }

        public String SolutionFilePath { get; }

        public IEnumerable<Artifact> Artifacts { get; }

    }

}

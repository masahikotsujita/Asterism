﻿using System;
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
        }

        public String Name { get; }

        public IEnumerable<String> Dependencies { get; }

        public String SolutionFilePath { get; }

    }

}

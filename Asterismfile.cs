using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Asterism {

public struct DependencyInfo {
    public string Project { get; set; }
    public string Version { get; set; }
}

public struct ArtifactsInfo {
    public IEnumerable<string> IncludeHeaders { get; set; }
    public IEnumerable<string> LinkLibraries { get; set; }
}

internal class Asterismfile {
    public Asterismfile(string filePath) {
        var reader = new StreamReader(filePath);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);
        var yaml = new Yaml(yamlStream.Documents[0].RootNode);

        Name = yaml["name"].String;

        Dependencies = yaml["dependencies"]
                       .List
                       ?.Select(yml => new DependencyInfo {
                           Project = yml["project"].String,
                           Version = yml["version"].String
                       });

        SolutionFilePath = yaml["sln_path"].String;

        var includeHeaders = yaml["artifacts"]["include_headers"].List?.Select(yml => yml.String);
        var linkLibraries = yaml["artifacts"]["link_libraries"].List?.Select(yml => yml.String);
        if (includeHeaders != null || linkLibraries != null) {
            ArtifactsInfo = new ArtifactsInfo {
                IncludeHeaders = includeHeaders,
                LinkLibraries = linkLibraries
            };
        }
    }

    public string Name { get; }

    public IEnumerable<DependencyInfo> Dependencies { get; }

    public string SolutionFilePath { get; }

    public ArtifactsInfo? ArtifactsInfo { get; }
}

}
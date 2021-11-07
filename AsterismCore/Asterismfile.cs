using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace AsterismCore {

public struct DependencyInfo {
    public string Project { get; set; }
    public string Version { get; set; }
}

public struct ArtifactsInfo {
    public IEnumerable<string> IncludeHeaders { get; set; }
    public IEnumerable<string> LinkLibraries { get; set; }
}

public class Asterismfile {
    public Asterismfile(string filePath) {
        var reader = new StreamReader(filePath);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);
        var yaml = new Yaml(yamlStream.Documents[0].RootNode);

        Name = yaml["name"].GetStringOrDefault();

        Dependencies = yaml["dependencies"].GetListOrDefault()
                       ?.Select(yml => new DependencyInfo {
                           Project = yml["project"].GetStringOrDefault(),
                           Version = yml["version"].GetStringOrDefault()
                       });

        SolutionFilePath = yaml["sln_path"].GetStringOrDefault();

        var includeHeaders = yaml["artifacts"]["include_headers"].GetListOrDefault()?.Select(yml => yml.GetStringOrDefault());
        var linkLibraries = yaml["artifacts"]["link_libraries"].GetListOrDefault()?.Select(yml => yml.GetStringOrDefault());
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
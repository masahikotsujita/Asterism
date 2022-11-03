using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace AsterismCore {

public class DependencyInfo {
    public string Project { get; set; }
    public string Version { get; set; }
}

public class ArtifactsInfo {
    public IEnumerable<string> IncludeHeaders { get; set; }
    public IEnumerable<string> LinkLibraries { get; set; }
}

public class Asterismfile {
    public string Name { get; set; }
    public string Version { get; set; }
    public IEnumerable<DependencyInfo> Dependencies { get; set; }
    public string SlnPath { get; set; }
    public ArtifactsInfo Artifacts { get; set; }
}

}
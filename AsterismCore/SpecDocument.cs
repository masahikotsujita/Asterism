using System.Collections.Generic;

namespace AsterismCore {

public class DependencyInSpec {
    public string Project { get; set; }
    public string Version { get; set; }
}

public class ArtifactsInSpec {
    public List<string> IncludeHeaders { get; set; }
    public List<string> LinkLibraries { get; set; }
}

public class SpecDocument {
    public string Name { get; set; }
    public string Version { get; set; }
    public List<DependencyInSpec> Dependencies { get; set; }
    public string SlnPath { get; set; }
    public ArtifactsInSpec Artifacts { get; set; }
}

}
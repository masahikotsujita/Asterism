using System.Collections.Generic;

namespace AsterismCore {

public class DependencyInLock {
    public string Project { get; set; }
    public string Revision { get; set; }
}

public class LockDocument {
    public string DocumentVersion { get; set; } = "0.1.0";
    public List<DependencyInLock> Dependencies { get; set; }
}

}
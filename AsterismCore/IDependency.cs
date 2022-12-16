using System.Collections.Generic;

namespace AsterismCore {

public interface IDependency<TDependency, TKey, TVersion, TRange>
    where TRange : IRange<TRange> {
    IEnumerable<(TDependency Module, TRange VersionConstraint)> GetRequirements(TVersion version);
    TVersion GetMaxSatisfyingVersionForConstraint(TRange range);
    bool IsSatisfiedVersion(TRange range, TVersion version);
    TKey Name { get; }
}

}
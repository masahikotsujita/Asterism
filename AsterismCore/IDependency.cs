using System.Collections.Generic;

namespace AsterismCore {

public interface IDependency<TDependency, TKey, TVersion, TRange>
    where TRange : IRange<TRange> {
    IEnumerable<(TDependency dependency, TRange range)> GetDependencies(TVersion version);
    TVersion GetMaxSatisfyingVersionForRange(TRange range);
    bool IsSatisfiedVersion(TRange range, TVersion version);
    TKey Key { get; }
}

}
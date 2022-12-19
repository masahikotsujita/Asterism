using System;
using System.Collections.Generic;

namespace AsterismCore {

public class Resolver<TDependency, TKey, TVersion, TRange>
    where TDependency : IDependency<TDependency, TKey, TVersion, TRange>
    where TRange : IRange<TRange>
    where TVersion : IEquatable<TVersion> {
    public Resolver(TDependency dependency) {
        Dependency = dependency;
    }

    public Dictionary<TKey, TVersion> Resolve() {
        var resolvedVersionsByKey = new Dictionary<TKey, TVersion>();
        do {
            var knownVersionRangesByKey = new Dictionary<TKey, TRange>();
            bool GetDependencies(TDependency parent, TVersion parentVersion) {
                foreach (var (dependency, dependencyVersionRangeByParent) in parent.GetDependencies(parentVersion)) {
                    // get dependencies for dependencies recursively...
                    if (knownVersionRangesByKey.TryGetValue(dependency.Key, out var existingRange)) {
                        knownVersionRangesByKey[dependency.Key] = existingRange.Intersect(dependencyVersionRangeByParent);
                    } else {
                        knownVersionRangesByKey[dependency.Key] = dependencyVersionRangeByParent;
                    }
                    var dependencyVersionRange = knownVersionRangesByKey[dependency.Key];
                    if (dependency.GetMaxSatisfyingVersionForRange(dependencyVersionRange) is not { } satisfiedVersion) {
                        throw new Exception($"Requirement (module: {dependency.Key} version: {dependencyVersionRangeByParent}) in module {parent.Key} cannot be satisfied.");
                    }
                    if (resolvedVersionsByKey.TryGetValue(dependency.Key, out var resolvedVersion) && !resolvedVersion.Equals(satisfiedVersion)) {
                        resolvedVersionsByKey[dependency.Key] = satisfiedVersion;
                        // pinned version was updated by narrower range, so try again from the beginning
                        return false;
                    }
                    resolvedVersionsByKey[dependency.Key] = satisfiedVersion;
                    if (!GetDependencies(dependency, satisfiedVersion)) {
                        return false;
                    }
                }
                return true;
            }
            if (GetDependencies(Dependency, default)) {
                break;
            }
        } while (true);
        return resolvedVersionsByKey;
    }

    public TDependency Dependency { get; init; }
}

}
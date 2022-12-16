using System;
using System.Collections.Generic;

namespace AsterismCore {

public class Resolver<TDependency, TKey, TVersion, TRange>
    where TDependency : IDependency<TDependency, TKey, TVersion, TRange>
    where TRange : IRange<TRange>
    where TVersion : IEquatable<TVersion> {
    public Resolver(TDependency rootModule) {
        RootModule = rootModule;
    }

    public Dictionary<TKey, TVersion> Resolve() {
        var resolvedVersionSpecifierByModuleName = new Dictionary<TKey, TVersion>();
        do {
            var versionConstraintsByModuleName = new Dictionary<TKey, TRange>();
            void GetDependencies(TDependency parentModule, TVersion parentModuleVersionSpecifier) {
                foreach (var requirement in parentModule.GetRequirements(parentModuleVersionSpecifier)) {
                    // get dependencies for dependencies recursively...
                    if (versionConstraintsByModuleName.TryGetValue(requirement.Module.Name, out var existingVersionConstraint)) {
                        versionConstraintsByModuleName[requirement.Module.Name] = existingVersionConstraint.Intersect(requirement.VersionConstraint);
                    } else {
                        versionConstraintsByModuleName[requirement.Module.Name] = requirement.VersionConstraint;
                    }
                    var versionConstraint = versionConstraintsByModuleName[requirement.Module.Name];
                    if (requirement.Module.GetMaxSatisfyingVersionForConstraint(versionConstraint) is not { } requirementVersionSpecifier) {
                        throw new Exception($"Requirement (module: {requirement.Module.Name} version: {requirement.VersionConstraint}) in module {parentModule.Name} cannot be satisfied.");
                    }
                    if (resolvedVersionSpecifierByModuleName.TryGetValue(requirement.Module.Name, out var resolvedVersionSpecifier) && !resolvedVersionSpecifier.Equals(requirementVersionSpecifier)) {
                        resolvedVersionSpecifierByModuleName[requirement.Module.Name] = requirementVersionSpecifier;
                        // pinned version was updated by narrower range, so try again from the beginning
                        continue;
                    } else {
                        resolvedVersionSpecifierByModuleName[requirement.Module.Name] = requirementVersionSpecifier;
                        GetDependencies(requirement.Module, requirementVersionSpecifier);
                    }
                }
            }
            GetDependencies(RootModule, default);
            break;
        } while (true);
        return resolvedVersionSpecifierByModuleName;
    }

    public TDependency RootModule { get; init; }
}

}
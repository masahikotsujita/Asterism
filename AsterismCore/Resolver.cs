using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SemanticVersioning;
using YamlDotNet.Serialization;
using Version = SemanticVersioning.Version;
using YamlDotNet.Serialization.NamingConventions;

namespace AsterismCore {

public class Resolver {
    public Resolver(Module rootModule) {
        RootModule = rootModule;
    }

    public Dictionary<string, VersionSpecifier> ResolveVersions() {
        var resolvedVersionSpecifierByModuleName = new Dictionary<string, VersionSpecifier>();
        do {
            var versionConstraintsByModuleName = new Dictionary<string, VersionConstraint>();
            void GetDependencies(Module parentModule, VersionSpecifier parentModuleVersionSpecifier) {
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
                    if (resolvedVersionSpecifierByModuleName.TryGetValue(requirement.Module.Name, out var resolvedVersionSpecifier) && resolvedVersionSpecifier != requirementVersionSpecifier) {
                        resolvedVersionSpecifierByModuleName[requirement.Module.Name] = requirementVersionSpecifier;
                        // pinned version was updated by narrower range, so try again from the beginning
                        continue;
                    } else {
                        resolvedVersionSpecifierByModuleName[requirement.Module.Name] = requirementVersionSpecifier;
                        GetDependencies(requirement.Module, requirementVersionSpecifier);
                    }
                }
            }
            GetDependencies(RootModule, VersionSpecifier.Default);
            break;
        } while (true);
        return resolvedVersionSpecifierByModuleName;
    }

    public Module RootModule { get; }
}

}
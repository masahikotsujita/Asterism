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
    private class ModuleGraph : Graph<string> {
        public ModuleGraph() {
            IncomingEdgesForNodes = new Dictionary<string, HashSet<string>>();
        }

        public Dictionary<string, HashSet<string>> IncomingEdgesForNodes { get; }
    }

    public Resolver(Module rootModule) {
        RootModule = rootModule;
    }

    public IEnumerable<(Module module, VersionSpecifier versionSpecifier)> ResolveVersions() {
        var moduleByModuleName = new Dictionary<string, Module> {
            [RootModule.Name] = RootModule
        };
        var resolvedVersionSpecifierByModuleName = new Dictionary<string, VersionSpecifier>();
        var graph = new ModuleGraph();
        do {
            graph.IncomingEdgesForNodes.Clear();
            graph.IncomingEdgesForNodes[RootModule.Name] = new HashSet<string>();
            var versionConstraintsByModuleName = new Dictionary<string, VersionConstraint>();
            void GetDependencies(Module parentModule, VersionSpecifier parentModuleVersionSpecifier) {
                foreach (var requirement in parentModule.GetRequirements(parentModuleVersionSpecifier)) {
                    // cache module...
                    moduleByModuleName[requirement.Module.Name] = requirement.Module;
                    // construct module graph...
                    if (!graph.IncomingEdgesForNodes.ContainsKey(requirement.Module.Name)) {
                        graph.IncomingEdgesForNodes[requirement.Module.Name] = new HashSet<string>();
                    }
                    graph.IncomingEdgesForNodes[requirement.Module.Name].Add(parentModule.Name);
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
        var topologicallySortedModuleNames = graph.TopologicalSort();
        return topologicallySortedModuleNames
               .Where(moduleName => moduleName != RootModule.Name)
               .Select(moduleName => (moduleByModuleName[moduleName], resolvedVersionSpecifierByModuleName[moduleName]));
    }

    private static ModuleGraph GraphFromModuleForNames(Dictionary<string, Module> modulesForNames) {
        var graph = new ModuleGraph();
        foreach (var moduleForName in modulesForNames) {
            graph.IncomingEdgesForNodes.Add(moduleForName.Key, new HashSet<string>());
        }
        foreach (var moduleForName in modulesForNames) {
            if (moduleForName.Value.SpecDocument.Dependencies != null) {
                foreach (var dependency in moduleForName.Value.SpecDocument.Dependencies) {
                    var moduleName = GetModuleNameFromProject(dependency.Project);
                    graph.IncomingEdgesForNodes[moduleName].Add(moduleForName.Key);
                }
            }
        }
        return graph;
    }

    private static string GetModuleNameFromProject(string project) {
        return project.Split('/')[1];
    }

    public Module RootModule { get; }
}

}
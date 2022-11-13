using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Build.Construction;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace AsterismCore {

public enum VersionSpecifierType {
    Version,
    Sha1
}

public struct VersionSpecifier {
    public VersionSpecifierType Type { get; set; }
    public Version Version { get; set; }
    public string Sha1 { get; set; }
}

public enum VersionConstraintType {
    Range,
    Sha1
}

public struct VersionConstraint {
    public VersionConstraintType Type { get; set; }
    public Range Range { get; set; }
    public string Sha1 { get; set; }
}

public struct Requirement {
    public string ModuleName { get; set; }
    public VersionConstraint VersionConstraint { get; set; }
}

public class Module {
    public Module(Context context, string name, string checkoutDirectoryPath, bool isRoot) {
        Context = context;
        Name = name;
        CheckoutDirectoryPath = checkoutDirectoryPath;
        Repository = new Repository(CheckoutDirectoryPath);
        IsRoot = isRoot;
    }

    public void LoadSpecFile() {
        var reader = File.OpenText(AsterismfilePath);
        var deserializer = new DeserializerBuilder()
                           .WithNamingConvention(UnderscoredNamingConvention.Instance)
                           .Build();
        SpecDocument = deserializer.Deserialize<SpecDocument>(reader);
        SolutionFilePath = Path.Combine(CheckoutDirectoryPath,
            FileUtility.ReplacePathSeparatorsForWindows(SpecDocument.SlnPath));
    }

    public void LoadSolutionFile() {
        SolutionFile = SolutionFile.Parse(SolutionFilePath);
    }

    public void CreatePropertySheet(bool forApplication, Dictionary<BuildConfiguration, List<string>> librariesForConfigurations) {
        var configurations = SolutionFile.SolutionConfigurations
                                         .Select(x => new BuildConfiguration(x));
        var relativePathFromSlnToArtifactsDir = FileUtility.GetRelativePath(SolutionFilePath, Context.ArtifactsDirectoryPath);
        var propertySheetPath = Path.Combine(AsterismDirectoryPath, "vsprops\\Asterism.props");
        var propertySheet = new PropertySheet();
        propertySheet.AddConfigurations(configurations);
        propertySheet.AddUserMacro("AsterismArtifactsDir", $"$(SolutionDir){relativePathFromSlnToArtifactsDir}");
        foreach (var configuration in configurations) {
            propertySheet.AddAdditionalIncludeDirectory($"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\", configuration);
        }
        if (forApplication) {
            foreach (var configuration in configurations) {
                propertySheet.AddAdditionalDependencies(librariesForConfigurations[configuration], configuration);
                propertySheet.AddAdditionalLibraryDirectory($"$(AsterismArtifactsDir){configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\", configuration);
            }
        }

        propertySheet.Save(propertySheetPath);
    }

    public bool Build(BuildConfiguration configuration, ref List<string> librariesToBeLinked) {
        var buildExitCode = MsBuildUtility.Build(SolutionFilePath, new Dictionary<string, string> { { "Platform", configuration.PlatformName }, { "Configuration", configuration.ConfigurationName } }, message => { Console.WriteLine(message); });
        if (buildExitCode != 0) {
            return false;
        }

        if (SpecDocument.Artifacts is ArtifactsInSpec artifacts) {
            var headerDestination = Path.Combine(Context.ArtifactsDirectoryPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\include\\");
            foreach (var headerPattern in artifacts.IncludeHeaders) {
                var headerSource = FileUtility.ReplacePathSeparatorsForWindows(headerPattern);
                var xcopyExitCode = FileUtility.XCopy(headerSource, headerDestination, CheckoutDirectoryPath, message => { Console.WriteLine(message); });
                if (xcopyExitCode != 0) {
                    return false;
                }
            }

            var libDestination = Path.Combine(Context.ArtifactsDirectoryPath, $"{configuration.PlatformName}\\{configuration.ConfigurationName}\\lib\\");
            foreach (var libraryPattern in artifacts.LinkLibraries) {
                var libSource = FileUtility.ReplacePathSeparatorsForWindows(libraryPattern).Replace("${PLATFORM}", configuration.PlatformName).Replace("${CONFIGURATION}", configuration.ConfigurationName);
                var lib = Path.GetFileName(libSource);
                var xcopyExitCode = FileUtility.XCopy(libSource, libDestination, CheckoutDirectoryPath, message => { Console.WriteLine(message); });
                if (xcopyExitCode != 0) {
                    return false;
                }

                librariesToBeLinked.Add(lib);
            }
        }

        return true;
    }

    public Context Context { get; }

    public string Name { get; }

    public string CheckoutDirectoryPath { get; }

    public Repository Repository { get; }

    public string AsterismDirectoryPath => Path.Combine(CheckoutDirectoryPath, ".asterism\\");

    public string AsterismfilePath => Path.Combine(CheckoutDirectoryPath, ".asterismfile.yml");

    public SpecDocument SpecDocument { get; private set; }

    public string SolutionFilePath { get; private set; }

    public SolutionFile SolutionFile { get; private set; }

    public bool IsFetched { get; set; }

    public string ProjectPath { get; set; }

    public bool IsRoot { get; }

    public bool IsLockMode { get; set; }

    public void EnsureCheckout(VersionSpecifier versionSpecifier) {
        switch (versionSpecifier.Type) {
        case VersionSpecifierType.Version:
            var tag = Repository.Tags
                                .Select(tag => Version.TryParse(tag.FriendlyName, out _) ? tag : null)
                                .First(tag => tag != null);
            if (Repository.Head.Tip.Sha != tag.Target.Sha) {
                Commands.Checkout(Repository, tag.CanonicalName);
            }
            break;
        case VersionSpecifierType.Sha1:
            if (Repository.Head.Tip.Sha != versionSpecifier.Sha1) {
                Commands.Checkout(Repository, versionSpecifier.Sha1);
            }
            break;
        }
    }

    private IEnumerable<Requirement> GetSpecRequirements() {
        LoadSpecFile();
        var requirements = new List<Requirement>();
        foreach (var dependency in SpecDocument.Dependencies) {
            var moduleName = GetModuleNameFromProject(dependency.Project);
            var range = Range.Parse(dependency.Version);
            var requirement = new Requirement {
                ModuleName = moduleName,
                VersionConstraint = new VersionConstraint {
                    Type = VersionConstraintType.Range,
                    Range = range
                }
            };
            requirements.Add(requirement);
        }
        return requirements;
    }

    private void EnsureLoadLockFile() {
        if (LockDocument != null) {
            return;
        }
        if (!File.Exists(Context.LockFilePath)) {
            return;
        }
        var reader = File.OpenText(Context.LockFilePath);
        var deserializer = new DeserializerBuilder()
                           .WithNamingConvention(UnderscoredNamingConvention.Instance)
                           .Build();
        LockDocument = deserializer.Deserialize<LockDocument>(reader);
    }

    private LockDocument LockDocument { get; set; }

    private IEnumerable<Requirement> GetLockRequirements() {
        if (LockDocument == null) {
            EnsureLoadLockFile();
        }
        var requirements = new List<Requirement>();
        foreach (var dependency in LockDocument.Dependencies) {
            var moduleName = GetModuleNameFromProject(dependency.Project);
            var sha1 = dependency.Revision;
            var requirement = new Requirement {
                ModuleName = moduleName,
                VersionConstraint = new VersionConstraint {
                    Type = VersionConstraintType.Sha1,
                    Sha1 = sha1
                }
            };
            requirements.Add(requirement);
        }
        return requirements;
    }

    public IEnumerable<Requirement> GetRequirements(VersionSpecifier? versionSpecifier) {
        if (!IsRoot) {
            Debug.Assert(versionSpecifier != null);
            EnsureCheckout(versionSpecifier.Value);
            if (IsLockMode) {
                return new List<Requirement>();
            }
            return GetSpecRequirements();
        }
        Debug.Assert(versionSpecifier == null);
        if (IsLockMode) {
            return GetLockRequirements();
        }
        return GetSpecRequirements();
    }

    public VersionSpecifier? GetMaxSatisfyingVersionForConstraints(IEnumerable<VersionConstraint> constraints) {
        var constraint = constraints.Aggregate((c1, c2) => c1.Type == VersionConstraintType.Range && c2.Type == VersionConstraintType.Range ? new VersionConstraint {
                    Type = VersionConstraintType.Range,
                    Range = c1.Range.Intersect(c2.Range)
                } : throw new ArgumentException());                           
        switch (constraint.Type) {
        case VersionConstraintType.Range:
            var maxSatisfyingVersionString = constraint.Range
                .MaxSatisfying(Repository.Tags.Select(tag => tag.FriendlyName), true, true);
            if (maxSatisfyingVersionString == null) {
                return null;
            }
            return new VersionSpecifier {
                Type = VersionSpecifierType.Version,
                Version = new Version(maxSatisfyingVersionString)
            };
        case VersionConstraintType.Sha1:
            return new VersionSpecifier {
                Type = VersionSpecifierType.Sha1,
                Sha1 = constraint.Sha1
            };
        default:
            throw new InvalidOperationException();
        }
    }

    public bool IsSatisfiedVersion(VersionConstraint constraint, VersionSpecifier versionSpecifier) {
        switch (constraint.Type) {
        case VersionConstraintType.Range:
            switch (versionSpecifier.Type) {
            case VersionSpecifierType.Version:
                return constraint.Range.IsSatisfied(versionSpecifier.Version, true);
            case VersionSpecifierType.Sha1:
                throw new InvalidOperationException();
            //var version = Repository.Tags
            //                    .Where(tag => tag.PeeledTarget.Sha == versionSpecifier.Sha1)
            //                    .Select(tag => Version.TryParse(tag.FriendlyName, out var version) ? version : null)
            //                    .FirstOrDefault(version => version != null && constraint.Range.IsSatisfied(version));
            //return version != null;
            default:
                throw new InvalidOperationException();
            }
        case VersionConstraintType.Sha1:
            return constraint.Sha1 == versionSpecifier.Sha1;
        default:
            throw new InvalidOperationException();
        }
    }

    private static string GetModuleNameFromProject(string project) {
        return project.Split('/')[1];
    }
}

}
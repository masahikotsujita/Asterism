using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Build.Construction;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace AsterismCore {

public enum VersionSpecifierType {
    Default,
    Version,
    Sha1
}

public struct VersionSpecifier : IEquatable<VersionSpecifier> {
    public override bool Equals(object obj) {
        return obj is VersionSpecifier other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine((int)Type, Version, Sha1);
    }

    public static VersionSpecifier Default => new() { Type = VersionSpecifierType.Default };
    public VersionSpecifierType Type { get; set; }
    public Version Version { get; set; }
    public string Sha1 { get; set; }

    public bool Equals(VersionSpecifier other) {
        if (Type != other.Type) {
            return false;
        }
        switch (Type) {
        case VersionSpecifierType.Default:
            return true;
        case VersionSpecifierType.Version:
            return Version == other.Version;
        case VersionSpecifierType.Sha1:
            return Sha1 == other.Sha1;
        default:
            throw new ArgumentOutOfRangeException();
        }
    }

    public static bool operator ==(VersionSpecifier a, VersionSpecifier b) {
        return a.Equals(b);
    }

    public static bool operator !=(VersionSpecifier a, VersionSpecifier b) {
        return !a.Equals(b);
    }
}

public enum VersionConstraintType {
    Range,
    Sha1
}

public struct VersionConstraint {
    public VersionConstraintType Type { get; set; }
    public Range Range { get; set; }
    public string Sha1 { get; set; }

    public VersionConstraint Intersect(VersionConstraint other) {
        if (Type != other.Type) {
            throw new ArgumentException("Cannot intersect version constraints with different version constraint type.");
        }
        switch (Type) {
        case VersionConstraintType.Range:
            return new VersionConstraint {
                Type = VersionConstraintType.Range,
                Range = Range.Intersect(other.Range)
            };
        case VersionConstraintType.Sha1:
            throw new ArgumentException("Cannot intersect sha version constraints");
        default:
            throw new ArgumentOutOfRangeException();
        }
    }
}

public struct Requirement {
    public Module Module { get; set; }
    public VersionConstraint VersionConstraint { get; set; }
}

public class Module {

    public Module(Context context, string name, bool isLockMode) {
        Context = context;
        IsRoot = true;
        IsLockMode = isLockMode;
        Name = name;
        CheckoutDirectoryPath = context.WorkingDirectoryPath;
    }

    public Module(Context context, bool isRoot, bool isLockMode, string projectPath) {
        Context = context;
        IsRoot = isRoot;
        IsLockMode = isLockMode;
        ProjectPath = projectPath;
        var moduleName = GetModuleNameFromProject(projectPath);
        Name = moduleName;
        CheckoutDirectoryPath = Path.Combine(context.CheckoutDirectoryPath, moduleName);
        if (!Directory.Exists(CheckoutDirectoryPath)) {
            var githubPath = $"https://github.com/{projectPath}.git";
            Repository.Clone(githubPath, CheckoutDirectoryPath);
            Repository = new Repository(CheckoutDirectoryPath);
        } else {
            Repository = new Repository(CheckoutDirectoryPath);
            var remote = Repository.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(Repository, remote.Name, refSpecs, null, "");
        }
    }

    private void LoadSpecFile() {
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
        case VersionSpecifierType.Default:
        default:
            throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerable<Requirement> GetSpecRequirements() {
        LoadSpecFile();
        var requirements = new List<Requirement>();
        if (SpecDocument.Dependencies != null) {
            foreach (var dependency in SpecDocument.Dependencies) {
                var moduleName = GetModuleNameFromProject(dependency.Project);
                var range = Range.Parse(dependency.Version);
                if (!Context.Caches.TryGetValue(moduleName, out var module)) {
                    module = new Module(Context, false, IsLockMode, dependency.Project);
                    Context.Caches[moduleName] = module;
                }
                var requirement = new Requirement {
                    Module = module,
                    VersionConstraint = new VersionConstraint {
                        Type = VersionConstraintType.Range,
                        Range = range
                    }
                };
                requirements.Add(requirement);
            }
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

    private IEnumerable<Requirement> GetLockRequirements() {
        if (LockDocument == null) {
            EnsureLoadLockFile();
        }
        var requirements = new List<Requirement>();
        foreach (var dependency in LockDocument.Dependencies) {
            var moduleName = GetModuleNameFromProject(dependency.Project);
            var sha1 = dependency.Revision;
            if (!Context.Caches.TryGetValue(moduleName, out var module)) {
                module = new Module(Context, false, IsLockMode, dependency.Project);
                Context.Caches[moduleName] = module;
            }
            var requirement = new Requirement {
                Module = module,
                VersionConstraint = new VersionConstraint {
                    Type = VersionConstraintType.Sha1,
                    Sha1 = sha1
                }
            };
            requirements.Add(requirement);
        }
        return requirements;
    }

    public IEnumerable<Requirement> GetRequirements(VersionSpecifier versionSpecifier) {
        if (IsRoot) {
            Debug.Assert(versionSpecifier.Type == VersionSpecifierType.Default);
            if (!IsLockMode) {
                return GetSpecRequirements();
            }
            return GetLockRequirements();
        }
        EnsureCheckout(versionSpecifier);
        if (!IsLockMode) {
            return GetSpecRequirements();
        }
        return new List<Requirement>();
    }

    public VersionSpecifier? GetMaxSatisfyingVersionForConstraint(VersionConstraint constraint) {
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

    public string GetSha1(VersionSpecifier versionSpecifier) {
        switch (versionSpecifier.Type) {
        case VersionSpecifierType.Version:
            var tagAndVersion = Repository.Tags
                                          .Select<Tag, (Tag tag, Version version)>(tag => (tag, Version.TryParse(tag.FriendlyName, out var version) ? version : null))
                                          .Where(tuple => tuple.version != null)
                                          .First(tuple => tuple.version == versionSpecifier.Version);
            return tagAndVersion.tag.PeeledTarget.Sha;
        case VersionSpecifierType.Sha1:
            return versionSpecifier.Sha1;
        default:
            throw new ArgumentOutOfRangeException();
        }
    }

    private static string GetModuleNameFromProject(string project) {
        return project.Split('/')[1];
    }

    private Context Context { get; }

    public string Name { get; }

    private string CheckoutDirectoryPath { get; }

    private Repository Repository { get; }

    public string AsterismDirectoryPath => Path.Combine(CheckoutDirectoryPath, ".asterism\\");

    public string AsterismfilePath => Path.Combine(CheckoutDirectoryPath, ".asterismfile.yml");

    public SpecDocument SpecDocument { get; private set; }

    public string SolutionFilePath { get; private set; }

    public SolutionFile SolutionFile { get; private set; }

    public string ProjectPath { get; }

    private bool IsRoot { get; }

    private bool IsLockMode { get; }

    private LockDocument LockDocument { get; set; }
}

}
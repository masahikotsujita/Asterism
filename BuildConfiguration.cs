using System;
using Microsoft.Build.Construction;

namespace Asterism {

public sealed class BuildConfiguration : IEquatable<BuildConfiguration> {
    public BuildConfiguration(string platformName, string configurationName) {
        PlatformName = platformName;
        ConfigurationName = configurationName;
    }

    public BuildConfiguration(SolutionConfigurationInSolution configuration) : this(configuration.PlatformName, configuration.ConfigurationName) {
    }

    public BuildConfiguration(ProjectConfigurationInSolution configuration) : this(configuration.PlatformName, configuration.ConfigurationName) {
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }
        if (ReferenceEquals(this, obj)) {
            return true;
        }
        if (obj.GetType() != GetType()) {
            return false;
        }
        return Equals((BuildConfiguration) obj);
    }

    public override int GetHashCode() {
        unchecked {
            return ((PlatformName != null ? PlatformName.GetHashCode() : 0) * 397) ^ (ConfigurationName != null ? ConfigurationName.GetHashCode() : 0);
        }
    }

    public string PlatformName { get; }
    public string ConfigurationName { get; }

    public bool Equals(BuildConfiguration other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }
        if (ReferenceEquals(this, other)) {
            return true;
        }
        return string.Equals(PlatformName, other.PlatformName) && string.Equals(ConfigurationName, other.ConfigurationName);
    }
}

}
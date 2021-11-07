using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AsterismCore {

public class PropertySheet {
    public PropertySheet() {
        UserMacros = new List<KeyValuePair<string, string>>();
        Configurations = new List<BuildConfiguration>();
        AdditionalDependencies = new Dictionary<BuildConfiguration, List<string>>();
        AdditionalLibraryDirectories = new Dictionary<BuildConfiguration, List<string>>();
        AdditionalIncludeDirectories = new Dictionary<BuildConfiguration, List<string>>();
    }

    public void Save(string filePath) {
        var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "Project", new XAttribute("ToolsVersion", "4.0"),
                new XElement(ns + "ImportGroup", new XAttribute("Label", "PropertySheets")),
                new XElement(ns + "PropertyGroup", new XAttribute("Label", "UserMacros"),
                    from userMacro in UserMacros
                    select new XElement(ns + userMacro.Key, userMacro.Value)
                ),
                new XElement(ns + "PropertyGroup"),
                from configuration in Configurations
                select new XElement(ns + "ItemDefinitionGroup", new XAttribute("Condition", $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'"),
                    new XElement(ns + "Link",
                        new XElement(ns + "AdditionalDependencies", AdditionalDependencies.GetValueOrDefault(configuration)?.Join(";") ?? ""),
                        new XElement(ns + "AdditionalLibraryDirectories", AdditionalLibraryDirectories.GetValueOrDefault(configuration)?.Join(";") ?? "")
                    ),
                    new XElement(ns + "ClCompile",
                        new XElement(ns + "AdditionalIncludeDirectories", AdditionalIncludeDirectories.GetValueOrDefault(configuration)?.Join(";") ?? "")
                    )
                ),
                new XElement(ns + "ItemGroup",
                    from userMacro in UserMacros
                    select new XElement(ns + "BuildMacro", new XAttribute("Include", userMacro.Key),
                        new XElement(ns + "Value", $"$({userMacro.Key})")
                    )
                )
            )
        );
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }
        doc.Save(filePath);
    }

    public void AddConfigurations(IEnumerable<BuildConfiguration> configurations) {
        Configurations.AddRange(configurations);
    }

    public void AddUserMacro(string key, string value) {
        UserMacros.Add(new KeyValuePair<string, string>(key, value));
    }

    public void AddAdditionalDependencies(IEnumerable<string> libraryNames, BuildConfiguration configuration) {
        if (!AdditionalDependencies.ContainsKey(configuration)) {
            AdditionalDependencies[configuration] = new List<string>();
        }
        AdditionalDependencies[configuration].AddRange(libraryNames);
    }

    public void AddAdditionalLibraryDirectories(IEnumerable<string> libraryDirectoryPaths, BuildConfiguration configuration) {
        if (!AdditionalLibraryDirectories.ContainsKey(configuration)) {
            AdditionalLibraryDirectories[configuration] = new List<string>();
        }
        AdditionalLibraryDirectories[configuration].AddRange(libraryDirectoryPaths);
    }

    public void AddAdditionalLibraryDirectory(string libraryDirectoryPath, BuildConfiguration configuration) {
        AddAdditionalLibraryDirectories(new[] { libraryDirectoryPath }, configuration);
    }

    public void AddAdditionalIncludeDirectories(IEnumerable<string> includePaths, BuildConfiguration configuration) {
        if (!AdditionalIncludeDirectories.ContainsKey(configuration)) {
            AdditionalIncludeDirectories[configuration] = new List<string>();
        }
        AdditionalIncludeDirectories[configuration].AddRange(includePaths);
    }

    public void AddAdditionalIncludeDirectory(string includePath, BuildConfiguration configuration) {
        AddAdditionalIncludeDirectories(new[] { includePath }, configuration);
    }

    private List<BuildConfiguration> Configurations { get; }

    private List<KeyValuePair<string, string>> UserMacros { get; }

    private Dictionary<BuildConfiguration, List<string>> AdditionalDependencies { get; }

    private Dictionary<BuildConfiguration, List<string>> AdditionalLibraryDirectories { get; }

    private Dictionary<BuildConfiguration, List<string>> AdditionalIncludeDirectories { get; }
}

}
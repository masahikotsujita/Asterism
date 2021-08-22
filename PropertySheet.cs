using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Asterism {

internal class PropertySheet {
    public PropertySheet() {
        UserMacros = new List<KeyValuePair<string, string>>();
        Configurations = new List<BuildConfiguration>();
        AdditionalDependencies = new Dictionary<BuildConfiguration, string>();
        AdditionalLibraryDirectories = new Dictionary<BuildConfiguration, string>();
        AdditionalIncludeDirectories = new Dictionary<BuildConfiguration, string>();
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
                        new XElement(ns + "AdditionalDependencies", AdditionalDependencies.GetValueOrDefault(configuration) ?? ""),
                        new XElement(ns + "AdditionalLibraryDirectories", AdditionalLibraryDirectories.GetValueOrDefault(configuration) ?? "")
                    ),
                    new XElement(ns + "ClCompile",
                        new XElement(ns + "AdditionalIncludeDirectories", AdditionalIncludeDirectories.GetValueOrDefault(configuration) ?? "")
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

    public List<KeyValuePair<string, string>> UserMacros { get; }

    public List<BuildConfiguration> Configurations { get; }

    public Dictionary<BuildConfiguration, string> AdditionalDependencies { get; }

    public Dictionary<BuildConfiguration, string> AdditionalLibraryDirectories { get; }

    public Dictionary<BuildConfiguration, string> AdditionalIncludeDirectories { get; }
}

}
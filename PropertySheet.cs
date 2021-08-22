using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Asterism {

internal class PropertySheet {
    public PropertySheet() {
        UserMacros = new List<KeyValuePair<string, string>>();
        Configurations = new List<string>();
        AdditionalDependencies = new Dictionary<string, string>();
        AdditionalLibraryDirectories = new Dictionary<string, string>();
        AdditionalIncludeDirectories = new Dictionary<string, string>();
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
                select new XElement(ns + "ItemDefinitionGroup", new XAttribute("Condition", $"'$(Configuration)|$(Platform)'=='{configuration}'"),
                    new XElement(ns + "Link",
                        new XElement(ns + "AdditionalDependencies", TryGetValueFromDictionary(AdditionalDependencies, configuration) ?? ""),
                        new XElement(ns + "AdditionalLibraryDirectories", TryGetValueFromDictionary(AdditionalLibraryDirectories, configuration) ?? "")
                    ),
                    new XElement(ns + "ClCompile",
                        new XElement(ns + "AdditionalIncludeDirectories", TryGetValueFromDictionary(AdditionalIncludeDirectories, configuration) ?? "")
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

    private static string TryGetValueFromDictionary(Dictionary<string, string> dictionary, string key) {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }
        return null;
    }

    public List<KeyValuePair<string, string>> UserMacros { get; }

    public List<string> Configurations { get; }

    public Dictionary<string, string> AdditionalDependencies { get; }

    public Dictionary<string, string> AdditionalLibraryDirectories { get; }

    public Dictionary<string, string> AdditionalIncludeDirectories { get; }
}

}
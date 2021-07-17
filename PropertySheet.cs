using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace Asterism {

    class PropertySheet {

        public PropertySheet() {
            UserMacros = new List<KeyValuePair<String, String>>();
            Configurations = new List<String>();
            AdditionalDependencies = new Dictionary<String, String>();
            AdditionalLibraryDirectories = new Dictionary<String, String>();
            AdditionalIncludeDirectories = new Dictionary<String, String>();
        }
        
        public List<KeyValuePair<String, String>> UserMacros { get; }

        public List<String> Configurations { get; }

        public Dictionary<String, String> AdditionalDependencies { get; }

        public Dictionary<String, String> AdditionalLibraryDirectories { get; }

        public Dictionary<String, String> AdditionalIncludeDirectories { get; }

        public void Save(String filePath) {
            var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
            XDocument doc = new XDocument(
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
        
        static String TryGetValueFromDictionary(Dictionary<String, String> dictionary, String key) {
            String value;
            if (dictionary.TryGetValue(key, out value)) {
                return value;
            } else {
                return null;
            }
        }

    }

}

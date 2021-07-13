using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Asterism {

    class PropertySheet {

        public PropertySheet() {
            UserMacros = new List<KeyValuePair<String, String>>();
        }

        public List<KeyValuePair<String, String>> UserMacros { get; }

        public String AdditionalDependencies { get; set; }

        public String AdditionalLibraryDirectories { get; set; }

        public String AdditionalIncludeDirectories { get; set; }

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
                    new XElement(ns + "ItemDefinitionGroup",
                        new XElement(ns + "Link",
                            new XElement(ns + "AdditionalDependencies", AdditionalDependencies ?? ""),
                            new XElement(ns + "AdditionalLibraryDirectories", AdditionalLibraryDirectories ?? "")
                        ),
                        new XElement(ns + "ClCompile",
                            new XElement(ns + "AdditionalIncludeDirectories", AdditionalIncludeDirectories ?? "")
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
            doc.Save(filePath);
        }

    }

}

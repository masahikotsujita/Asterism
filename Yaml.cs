using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Asterism {

    struct YAML {

        public YAML(YamlNode yamlNode) {
            this.YamlNode = yamlNode;
        }

        private YamlNode YamlNode { get; }

        public YAML this[String key] {
            get => GetNodeForKey(key);
        }

        public YAML this[int index] {
            get => GetNodeAtIndex(index);
        }

        public YAML GetNodeForKey(String key) {
            if (this.YamlNode == null) {
                return new YAML(null);
            }
            if (this.YamlNode.NodeType != YamlNodeType.Mapping) {
                return new YAML(null);
            }
            var mappingNode = (YamlMappingNode)this.YamlNode;
            if (!mappingNode.Children.ContainsKey(key))
            {
                return new YAML(null);
            }
            var childNode = mappingNode.Children[key];
            return new YAML(childNode);
        }

        public YAML GetNodeAtIndex(int index) {
            if (this.YamlNode == null) {
                return new YAML(null);
            }
            if (this.YamlNode.NodeType != YamlNodeType.Sequence) {
                return new YAML(null);
            }
            var sequenceNode = (YamlSequenceNode)this.YamlNode;
            if (index >= sequenceNode.Count()) {
                return new YAML(null);
            }
            var childNode = sequenceNode.Children[index];
            return new YAML(childNode);
        }

        public String String {
            get {
                if (this.YamlNode == null) {
                    return null;
                }
                if (this.YamlNode.NodeType != YamlNodeType.Scalar) {
                    return null;
                }
                return ((YamlScalarNode)YamlNode).Value;
            }
        }

        public IEnumerable<YAML> List {
            get {
                if (this.YamlNode == null) {
                    return null;
                }
                if (this.YamlNode.NodeType != YamlNodeType.Sequence) {
                    return null;
                }
                var sequenceNode = (YamlSequenceNode)this.YamlNode;
                var results = new List<YAML>();
                foreach (var node in sequenceNode) {
                    results.Add(new YAML(node));
                }
                return results;
            }
        }

    }

}

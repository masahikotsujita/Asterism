using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Asterism {

    struct Yaml {

        public Yaml(YamlNode yamlNode) {
            this.YamlNode = yamlNode;
        }

        private YamlNode YamlNode { get; }

        public Yaml this[String key] {
            get => GetNodeForKey(key);
        }

        public Yaml this[int index] {
            get => GetNodeAtIndex(index);
        }

        public Yaml GetNodeForKey(String key) {
            if (this.YamlNode == null) {
                return new Yaml(null);
            }
            if (this.YamlNode.NodeType != YamlNodeType.Mapping) {
                return new Yaml(null);
            }
            var mappingNode = (YamlMappingNode)this.YamlNode;
            var childNode = mappingNode.Children[key];
            return new Yaml(childNode);
        }

        public Yaml GetNodeAtIndex(int index) {
            if (this.YamlNode == null) {
                return new Yaml(null);
            }
            if (this.YamlNode.NodeType != YamlNodeType.Sequence) {
                return new Yaml(null);
            }
            var sequenceNode = (YamlSequenceNode)this.YamlNode;
            if (index >= sequenceNode.Count()) {
                return new Yaml(null);
            }
            var childNode = sequenceNode.Children[index];
            return new Yaml(childNode);
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

        public IEnumerable<Yaml> List {
            get {
                if (this.YamlNode == null) {
                    return null;
                }
                if (this.YamlNode.NodeType != YamlNodeType.Sequence) {
                    return null;
                }
                var sequenceNode = (YamlSequenceNode)this.YamlNode;
                var results = new List<Yaml>();
                foreach (var node in sequenceNode) {
                    results.Add(new Yaml(node));
                }
                return results;
            }
        }

    }

}

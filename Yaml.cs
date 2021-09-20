using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Asterism {

internal struct Yaml {
    public Yaml(YamlNode yamlNode) {
        YamlNode = yamlNode;
    }

    private YamlNode YamlNode { get; }

    public Yaml this[string key] => GetChild(key);

    public Yaml this[int index] => GetChild(index);

    public Yaml GetChild(string key) {
        if (YamlNode == null) {
            return new Yaml(null);
        }
        if (YamlNode.NodeType != YamlNodeType.Mapping) {
            return new Yaml(null);
        }
        var mappingNode = (YamlMappingNode) YamlNode;
        if (!mappingNode.Children.ContainsKey(key)) {
            return new Yaml(null);
        }
        var childNode = mappingNode.Children[key];
        return new Yaml(childNode);
    }

    public Yaml GetChild(int index) {
        if (YamlNode == null) {
            return new Yaml(null);
        }
        if (YamlNode.NodeType != YamlNodeType.Sequence) {
            return new Yaml(null);
        }
        var sequenceNode = (YamlSequenceNode) YamlNode;
        if (index >= sequenceNode.Count()) {
            return new Yaml(null);
        }
        var childNode = sequenceNode.Children[index];
        return new Yaml(childNode);
    }

    public string GetStringOrDefault() {
        if (YamlNode == null) {
            return null;
        }
        if (YamlNode.NodeType != YamlNodeType.Scalar) {
            return null;
        }
        return ((YamlScalarNode) YamlNode).Value;
    }

    public List<Yaml> GetListOrDefault() {
        if (YamlNode == null) {
            return null;
        }
        if (YamlNode.NodeType != YamlNodeType.Sequence) {
            return null;
        }
        var sequenceNode = (YamlSequenceNode) YamlNode;
        var results = new List<Yaml>();
        foreach (var node in sequenceNode) {
            results.Add(new Yaml(node));
        }
        return results;
    }
}

}
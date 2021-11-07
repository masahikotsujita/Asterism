using System;
using System.Collections.Generic;
using System.Linq;

namespace AsterismCore {

public interface Graph<TNode> where TNode : IEquatable<TNode> {
    Dictionary<TNode, HashSet<TNode>> IncomingEdgesForNodes { get; }
}

public static class GraphExtension {
    public static IEnumerable<Node> TopologicalSort<Node>(this Graph<Node> graph) where Node : IEquatable<Node> {
        // Perform topological sort using Kahn's Algorithm
        // https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm
        var l = new List<Node>();
        var s = graph.IncomingEdgesForNodes
                     .Where(nie => nie.Value.Count == 0)
                     .Select(nie => nie.Key)
                     .ToList();
        // Clone the graph to work
        var workGraph = new Dictionary<Node, HashSet<Node>>();
        foreach (var nie in graph.IncomingEdgesForNodes) {
            var node = nie.Key;
            var incomingEdges = nie.Value;
            workGraph.Add(node, new HashSet<Node>(incomingEdges));
        }

        while (s.Count > 0) {
            var n = s[0];
            s.RemoveAt(0);
            l.Add(n);
            foreach (var m in workGraph) {
                if (m.Value.Contains(n)) {
                    m.Value.Remove(n);
                    if (m.Value.Count == 0) {
                        s.Add(m.Key);
                    }
                }
            }
            workGraph.Remove(n);
        }

        l.Reverse();

        return workGraph.Count > 0 ? null : l;
    }
}

}
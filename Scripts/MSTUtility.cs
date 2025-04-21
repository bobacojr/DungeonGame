using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration
{
    public static class MSTUtility
    {
        public static HashSet<Edge> ExtractEdgesFromTriangles(List<DelaunayTriangulation.Triangle> triangles)
        {
            HashSet<Edge> edges = new HashSet<Edge>();

            foreach (var triangle in triangles)
            {
                edges.Add(new Edge(triangle.A, triangle.B));
                edges.Add(new Edge(triangle.B, triangle.C));
                edges.Add(new Edge(triangle.C, triangle.A));
            }

            return edges;
        }

        public static HashSet<Edge> CreateMinimumSpanningTree(HashSet<Edge> edges)
        {
            // Convert edges to list and sort by length
            List<Edge> sortedEdges = edges.ToList();
            sortedEdges.Sort((a, b) => a.Length.CompareTo(b.Length));

            // Kruskal's algorithm for MST
            HashSet<Edge> mstEdges = new HashSet<Edge>();
            Dictionary<Vector2, Vector2> parent = new Dictionary<Vector2, Vector2>();

            // Find parent with path compression
            Vector2 FindParent(Vector2 v)
            {
                if (!parent.ContainsKey(v)) parent[v] = v;
                if (parent[v] != v) parent[v] = FindParent(parent[v]);
                return parent[v];
            }

            foreach (Edge edge in sortedEdges)
            {
                Vector2 rootA = FindParent(edge.A);
                Vector2 rootB = FindParent(edge.B);

                if (rootA != rootB)
                {
                    mstEdges.Add(edge);
                    parent[rootB] = rootA; // Union
                }
            }

            return mstEdges;
        }
    }
}

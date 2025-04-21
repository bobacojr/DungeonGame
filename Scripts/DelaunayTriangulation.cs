using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DelaunayTriangulation
{   
    /* Define the structure of a triangle */
    public struct Triangle
    {
        public Vector2 A, B, C; // Every triangle has 3 Vector2 vertices

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;
        }

        public bool ContainsVertex(Vector2 v)
        {
            return v == A || v == B || v == C;
        }

        /* Checks if a vertex lies in a triangles circumcircle (violating Delaunays Triangulation) */
        public bool InCircumcircle(Vector2 v)
        {
            // Localize the vertices so v is the origin
            Vector2 a = A - v;
            Vector2 b = B - v;
            Vector2 c = C - v;

            // Calculate the determinant (positive = inside circumcircle, negative = outside circumcircle)
            float det = a.x * (b.y * c.sqrMagnitude - c.y * b.sqrMagnitude) -
                        a.y * (b.x * c.sqrMagnitude - c.x * b.sqrMagnitude) +
                        a.sqrMagnitude * (b.x * c.y - c.x * b.y);
            return det > 0;
        }
    }

    public struct Edge
    {
        public Vector2 U, V;

        public Edge(Vector2 u, Vector2 v)
        {
            U = u;
            V = v;
        }

        /* Bidirectional comparison for edges */
        public bool Equals(Edge other)
        {
            return U == other.U && V == other.V || U == other.V && V == other.U;
        }
    }

    private static void AddEdgeIfUnique(List<Edge> edges, Edge edge)
    {
        bool found = false;
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].Equals(edge))
            {
                edges.RemoveAt(i);
                found = true;
                break;
            }
        }
        if (!found)
        {
            edges.Add(edge);
        }
    }

    /* Bowyer-Watson algorithm to create Delaunay Triangulation (List<Vector2> points are the center positions of the room prefabs) */
    public static List<Triangle> BowyerWatson(List<Vector2> points)
    {
        List<Triangle> triangulation = new(); // List of triangles 

        // Create a super triangle to hold all verticies
        float minX = Mathf.Infinity, minY = Mathf.Infinity;
        float maxX = -Mathf.Infinity, maxY = -Mathf.Infinity;

        // Update the bounds of the super triangle
        foreach (Vector2 p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;
        float margin = Mathf.Max(width, height) * 10f;

        Vector2 A = new Vector2(minX - margin, minY - margin); // Bottom left vertex
        Vector2 B = new Vector2(maxX + margin, minY - margin); // Bottom right vertex
        Vector2 C = new Vector2(minX + width / 2, maxY + margin); // Top vertex

        Triangle superTriangle = new Triangle(A, B, C);
        triangulation.Add(superTriangle); // Add the super triangle as the first triangle

        foreach (Vector2 point in points)
        {
            List<Triangle> badTriangles = new(); // Initialize a new bad triangles list

            /* Find all triangles where the point lies in their circumcircle */
            foreach (Triangle triangle in triangulation)
            {
                if (triangle.InCircumcircle(point))
                {
                    badTriangles.Add(triangle);
                }
            }

            List<Edge> polygon = new();

            /* Find boundry of polygonal hole */
            foreach(Triangle triangle in badTriangles)
            {
                Edge edge1 = new Edge(triangle.A, triangle.B);
                Edge edge2 = new Edge(triangle.B, triangle.C);
                Edge edge3 = new Edge(triangle.C, triangle.A);

                AddEdgeIfUnique(polygon, edge1);
                AddEdgeIfUnique(polygon, edge2);
                AddEdgeIfUnique(polygon, edge3);
            }

            /* Remove all bad triangles */
            foreach (Triangle badTriangle in badTriangles)
            {
                triangulation.Remove(badTriangle);
            }

            /* Re-triangulate the polygonal hole  */
            foreach (Edge edge in polygon)
            {
                triangulation.Add(new Triangle(edge.U, edge.V, point));
            }
        }
        triangulation.RemoveAll(
            triangle => triangle.ContainsVertex(A) || 
            triangle.ContainsVertex(B) || 
            triangle.ContainsVertex(C)
        );

        return triangulation;
    }
}

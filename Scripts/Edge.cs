using UnityEngine;

namespace DungeonGeneration
{
    public struct Edge
    {
        public Vector2 A, B; // A and B are edge positions
        public float Length => Vector2.Distance(A, B); // Length of the edge

        public Edge(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }
    }
}

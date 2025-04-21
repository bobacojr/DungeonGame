using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    public class Node
    {
        public bool walkable; // AStarGrids obstacle detection
        public Vector3 worldPos; // The actual world position taken from AStarGrid
        public int gridX, gridY; // Grid coordinates taken from AStarGrid
        public int gCost, hCost; // gCost - distance from start node, hCost - heuristic distance to target
        public Node parent; // For reconstructing the path

        public int FCost => gCost + hCost; // Total cost for node prioritization

        public Node(bool walkable, Vector3 worldPos, int gridX, int gridY)
        {
            this.walkable = walkable;
            this.worldPos = worldPos;
            this.gridX = gridX;
            this.gridY = gridY;
        }
    }

    public static List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos, AStarGrid grid)
    {
        Node startNode = grid.WorldToNode(startPos);
        Node endNode = grid.WorldToNode(targetPos);

        if (startNode == null || endNode == null || !startNode.walkable || !endNode.walkable)
        {
            Debug.LogWarning("No valid start or target node found.");
            return null;
        }

        List<Node> openSet = new() { startNode };
        HashSet<Node> closedSet = new();

        const int maxIterations = 6000;
        int iterations = 0;

        while (openSet.Count > 0 && iterations++ < maxIterations)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == endNode)
            {
                return RetracePath(startNode, endNode);
            }

            foreach (Node neighbor in GetOrthogonalNeighbors(currentNode, grid.Grid))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int moveCost = currentNode.gCost + GetManhattanDistance(currentNode, neighbor);

                Vector3 toTarget = (endNode.worldPos - currentNode.worldPos).normalized;
                Vector3 toNeighbor = (neighbor.worldPos - currentNode.worldPos).normalized;

                bool isMovingInTargetDir = Vector3.Dot(toTarget, toNeighbor) > 0.9f;
                if (!isMovingInTargetDir)
                    moveCost += 25; // penalize turns

                // If it's the exact end node, give bonus to pathfinding to reach it exactly
                if (neighbor == endNode)
                    neighbor.hCost = -1000;

                if (moveCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = moveCost;
                    neighbor.hCost = GetManhattanDistance(neighbor, endNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private static List<Node> GetOrthogonalNeighbors(Node node, Node[,] grid)
    {
        List<Node> neighbors = new List<Node>();
        
        // Check four directions only (no diagonals)
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int checkX = node.gridX + dx[i];
            int checkY = node.gridY + dy[i];

            if (checkX >= 0 && checkX < grid.GetLength(0) && checkY >= 0 && checkY < grid.GetLength(1))
            {
                neighbors.Add(grid[checkX, checkY]);
            }
        }
        return neighbors;
    }

    private static int GetManhattanDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        return 10 * (dstX + dstY); // Uniform cost for all orthogonal moves
    }

    private static List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode.worldPos);
            if (currentNode == startNode)
                break;

            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }
}
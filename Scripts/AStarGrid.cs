using UnityEngine;
using System.Collections.Generic;

public class AStarGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public LayerMask obstacleMask; // Define obstacles
    public LayerMask doorMask;
    public Vector2 gridWorldSize = new Vector2(60, 60); // Grid size is the same as dungeon map size
    public float nodeRadius = 1f; // Radius of our grid nodes
    
    [Header("Debug")]
    public bool showGrid = true;
    public Color walkableColor = Color.green;
    public Color obstacleColor = Color.red;

    public AStar.Node[,] GridNodes { get { return grid; } } // Readonly property to access the grid
    public float NodeDiameter { get { return nodeRadius * 2; } } // Calculates the nodes diameter for easy access

    private HashSet<Vector2Int> doorNodes = new();

    private AStar.Node[,] grid;
    public AStar.Node[,] Grid {
        get {
            if (grid == null) CreateGrid();
            return grid;
        }
    }

    void Awake()
    {
        nodeRadius = Mathf.Min(nodeRadius, 1f); // 
        CreateGrid();
    }

    public void CreateGrid()
    {

        if (doorMask == 0) 
        {
            doorMask = LayerMask.GetMask("Doors"); // Find the "Door" layer dynamically
        }

        int gridSizeX = Mathf.RoundToInt(gridWorldSize.x / (nodeRadius * 2));
        int gridSizeY = Mathf.RoundToInt(gridWorldSize.y / (nodeRadius * 2));

        gridSizeX = Mathf.Max(gridSizeX, 60);
        gridSizeY = Mathf.Max(gridSizeY, 60);
        
        grid = new AStar.Node[gridSizeX, gridSizeY];

        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2; // Bottom left corner of the grid/map

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * NodeDiameter) + Vector3.forward * (y * NodeDiameter);
                                
                // nodeRadius * 0.8f : node is walkable if no obstacles within 80% of its radius
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius * 1.0f, obstacleMask);

                grid[x, y] = new AStar.Node(walkable, worldPoint, x, y);

                if (Physics.CheckSphere(worldPoint, nodeRadius * 1.0f, doorMask))
                {
                    doorNodes.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    /* Converts grid position to (x, y) coordinates */
    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;
        
        return worldBottomLeft + Vector3.right * (x * (nodeRadius * 2) + nodeRadius) + Vector3.forward * (y * (nodeRadius * 2) + nodeRadius);
    }

    public AStar.Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        return WorldToNode(worldPosition); 
    }

    public AStar.Node GetNearestWalkable(Vector3 worldPosition)
    {
        AStar.Node nearestNode = WorldToNode(worldPosition);
        if (nearestNode.walkable) return nearestNode;

        // Search nearby nodes if center isn't walkable
        int searchRadius = 3;
        float minDistance = Mathf.Infinity;

        int baseX = nearestNode.gridX;
        int baseY = nearestNode.gridY;

        for (int x = Mathf.Max(0, baseX-searchRadius); x <= Mathf.Min(grid.GetLength(0)-1, baseX+searchRadius); x++)
        {
            for (int y = Mathf.Max(0, baseY-searchRadius); y <= Mathf.Min(grid.GetLength(1)-1, baseY+searchRadius); y++)
            {
                if (grid[x,y].walkable)
                {
                    float dist = Vector3.Distance(worldPosition, grid[x,y].worldPos);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestNode = grid[x,y];
                    }
                }
            }
        }

        return nearestNode;
    }

    public AStar.Node WorldToNode(Vector3 worldPosition)
    {
        Vector3 bottomLeft = transform.position - 
                            Vector3.right * gridWorldSize.x / 2f -
                            Vector3.forward * gridWorldSize.y / 2f;

        float dx = worldPosition.x - bottomLeft.x;
        float dz = worldPosition.z - bottomLeft.z;

        int x = Mathf.FloorToInt(dx / NodeDiameter);
        int y = Mathf.FloorToInt(dz / NodeDiameter);

        x = Mathf.Clamp(x, 0, grid.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, grid.GetLength(1) - 1);

        AStar.Node node = grid[x, y];

        return node;
    }

    public bool IsInGridBounds(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        return Mathf.Abs(localPos.x) < gridWorldSize.x/2 && 
               Mathf.Abs(localPos.z) < gridWorldSize.y/2;
    }

    public void ValidateGrid()
    {
        int inconsistencies = 0;
        foreach (AStar.Node node in grid)
        {
            bool walkable = !Physics.CheckBox(node.worldPos, Vector3.one * (nodeRadius * 1f), Quaternion.identity, obstacleMask);

            if (node.walkable != walkable)
            {
                inconsistencies++;
                Debug.DrawRay(node.worldPos, Vector3.up * 3, Color.red, 10f);
            }
        }
        if (inconsistencies > 0)
        {
            Debug.LogWarning($"Grid has {inconsistencies} walkability inconsistencies");
        }
    }

    /*
    void OnDrawGizmos()
    {
        if (!showGrid || grid == null) return;

        foreach (AStar.Node node in grid)
        {
            Vector2Int gridPos = new Vector2Int(node.gridX, node.gridY);

            if (doorNodes.Contains(gridPos))
            {
                Gizmos.color = Color.blue; // Door floor tile color
            }
            else
            {
                Gizmos.color = node.walkable ? walkableColor : obstacleColor;
            }

            Gizmos.DrawWireCube(node.worldPos, Vector3.one * (nodeRadius * 1f));
        }
    }
    */
}
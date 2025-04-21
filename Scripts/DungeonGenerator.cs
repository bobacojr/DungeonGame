using System.Collections.Generic;
using System.Collections; 
using UnityEngine;
using DungeonGeneration;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class Generator : MonoBehaviour
{
    public static Generator Instance;
    [Header("Pathfinding Settings")]
    public GeneratorSettings settings;

    [Header("Parent for all dungeon geometry")]
    public Transform dungeonRoot;

    [Header("Navmesh Surface")]
    public NavMeshSurface navSurface;

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public int minEnemiesPerRoom = 1;
    public int maxEnemiesPerRoom = 3;
    public float enemySpawnPadding = 1f;

    [Header("Wave Settings")]
    public int currentWave = 1;
    public int enemiesPerWave = 5;
    private bool dungeonGenerated = false;
    private bool waveInProgress = false;

    private List<RoomData> placedRoomsData = new List<RoomData>();
    private HashSet<Vector3> placedCorridorTiles = new HashSet<Vector3>();
    private HashSet<Transform> connectedDoors = new HashSet<Transform>();
    private List<GameObject> activeEnemies = new();

    /*
    void Start()
    {
        GenerateDungeon(); // Generate the dungeon on start
        Debug.Log($"Placed {placedRoomsData.Count}/{settings.numberOfRooms}");
        navSurface.BuildNavMesh();
        SpawnPlayerInFirstRoom();
        SpawnAllEnemies();
    }
    */

    public void BeginGame()
    {
        if (!dungeonGenerated)
        {
            GenerateDungeon();               // Generate rooms only ONCE
            navSurface.BuildNavMesh();
            SpawnPlayerInFirstRoom();
            dungeonGenerated = true;
        }

        StartWave();
    }

    void Awake()
    {
        Instance = this;
    }

    void StartWave()
    {
        waveInProgress = true;
        activeEnemies.Clear();

        Debug.Log($"Starting Wave {currentWave}");

        for (int i = 1; i < placedRoomsData.Count; i++)
        {
            SpawnEnemiesInRoom(placedRoomsData[i]);
        }
    }

    void Update()
    {
        if (waveInProgress)
        {
            int enemyCount = GameObject.FindObjectsOfType<EnemyController>().Length;

            if (UIManager.Instance != null)
                UIManager.Instance.UpdateEnemyCount(enemyCount);  // ✅ Update enemy count

            if (enemyCount == 0)
            {
                waveInProgress = false;
                StartCoroutine(StartNextWave());
            }
        }
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        currentWave = 1;
        waveInProgress = false;
        dungeonGenerated = false;

        // Destroy all enemies
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(enemy);

        // Destroy the player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            Destroy(player);

        // Destroy old dungeon geometry
        foreach (Transform child in dungeonRoot)
            Destroy(child.gameObject);

        placedRoomsData.Clear();
        placedCorridorTiles.Clear();
        connectedDoors.Clear();
    }

    IEnumerator StartNextWave()
    {
        currentWave += 1;
        UIManager.Instance.UpdateWave(currentWave);

        yield return new WaitForSeconds(2f);
        StartWave();
    }

    GameObject SpawnEnemyInRoom(RoomData room)
    {
        BoxCollider col = room.roomObject.GetComponent<BoxCollider>();
        Vector3 center = room.roomObject.transform.TransformPoint(col.center);
        Vector3 half = col.size * 0.5f - new Vector3(enemySpawnPadding, 0, enemySpawnPadding);

        Vector3 randXZ = new Vector3(
            Random.Range(-half.x, half.x),
            0,
            Random.Range(-half.z, half.z)
        );
        Vector3 samplePos = center + randXZ;

        if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
        {
            var e = Instantiate(enemyPrefab, hit.position, Quaternion.identity, dungeonRoot);
            if (!e.TryGetComponent<NavMeshAgent>(out _))
                e.AddComponent<NavMeshAgent>();
            return e;
        }

        return null;
    }

    void GenerateDungeon()
    {   
        /* Create the grid used for the A* algorithm */
        AStarGrid grid = gameObject.AddComponent<AStarGrid>();
        grid.gridWorldSize = settings.mapSize + new Vector2(4f, 4f); // Add some padding
        grid.nodeRadius = settings.corridorWidth / 2f;
        grid.obstacleMask = settings.roomWallsMask;

        // Place spawn room with player
        if (MapUtility.TryPlacingRoom(settings.spawnRoomPrefab, placedRoomsData, settings.mapSize, settings.roomPadding, settings.maxPlacementAttempts, settings, out RoomData spawnRoom))
        {
            PlaceUnderRoot(spawnRoom.roomObject);
        }

        /* Randomly place rooms */
        for (int i = 0; i < settings.numberOfRooms; i++)
        {
            GameObject randomRoom = settings.roomPrefabs[Random.Range(0, settings.roomPrefabs.Count)];
            if (MapUtility.TryPlacingRoom(randomRoom, placedRoomsData, settings.mapSize, settings.roomPadding, settings.maxPlacementAttempts, settings, out RoomData newRoom))
            {
                PlaceUnderRoot(newRoom.roomObject);
                Debug.Log($"Spawning enemies in room: {newRoom.roomObject.name}");
                //SpawnEnemiesInRoom(newRoom);
            }
        }

        /* Get the center position of each room */
        List<Vector2> roomCenters = new();
        foreach (RoomData room in placedRoomsData)
        {
            BoxCollider collider = room.roomObject.GetComponent<BoxCollider>();
            Vector2 center = new Vector2(
                room.position.x + collider.center.x, 
                room.position.z + collider.center.z
            );
            roomCenters.Add(center);
        }
        
        grid.CreateGrid();

        List<DelaunayTriangulation.Triangle> triangles = DelaunayTriangulation.BowyerWatson(roomCenters);
        HashSet<Edge> edges = MSTUtility.ExtractEdgesFromTriangles(triangles);
        HashSet<Edge> mstEdges = MSTUtility.CreateMinimumSpanningTree(edges);

        VisualizeTriangulation(triangles);
        
        CorridorGenerator.GenerateCorridors(grid, mstEdges, placedRoomsData, placedCorridorTiles, connectedDoors, settings, dungeonRoot);

        CorridorGenerator.PlaceWallsAroundCorridors(placedCorridorTiles, connectedDoors, settings, dungeonRoot);

        DoorUtility.FinalizeDoors(placedRoomsData, connectedDoors);
    }

    private void SpawnPlayerInFirstRoom()
    {
        RoomData spawnRoom = placedRoomsData[0];
        BoxCollider col = spawnRoom.roomObject.GetComponent<BoxCollider>();
        Vector3 worldCenter = spawnRoom.position + col.center;
        Vector3 halfSize = col.size * 0.5f;
        float x = Random.Range(-halfSize.x, halfSize.x);
        float z = Random.Range(-halfSize.z, halfSize.z);
        Vector3 spawnPos = worldCenter + new Vector3(x, 1f, z);
        Instantiate(settings.playerPrefab, spawnPos, Quaternion.identity);
    }

    void SpawnEnemiesInRoom(RoomData room)
    {
        BoxCollider col = room.roomObject.GetComponent<BoxCollider>();
        Vector3 center = room.roomObject.transform.TransformPoint(col.center);
        Vector3 half = col.size * 0.5f - new Vector3(enemySpawnPadding, 0, enemySpawnPadding);

        // Increase spawn count with each wave
        int count = Random.Range(minEnemiesPerRoom + currentWave, maxEnemiesPerRoom + currentWave + 1);

        Debug.Log($"→ Attempting to spawn {count} enemies in {room.roomObject.name}");

        for (int i = 0; i < count; i++)
        {
            Vector3 randXZ = new Vector3(
                Random.Range(-half.x, half.x),
                0,
                Random.Range(-half.z, half.z)
            );
            Vector3 samplePos = center + randXZ;

            if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                var e = Instantiate(enemyPrefab, hit.position, Quaternion.identity, dungeonRoot);
                if (!e.TryGetComponent<NavMeshAgent>(out _))
                    e.AddComponent<NavMeshAgent>();
            }
            else
            {
                Debug.Log($"X no NavMesh near {samplePos}");
            }
        }
    }

    /*
    void SpawnAllEnemies()
    {
        for (int i = 1; i < placedRoomsData.Count; i++)
        {
            SpawnEnemiesInRoom(placedRoomsData[i]);
        }
    }
    */

    /*
    void SpawnEnemiesInRoom(RoomData room)
    {
        // get the room’s center in world space
        var col = room.roomObject.GetComponent<BoxCollider>();
        Vector3 center = room.roomObject.transform.TransformPoint(col.center);
        // inset by padding
        Vector3 half = col.size * 0.5f - new Vector3(enemySpawnPadding, 0, enemySpawnPadding);

        int count = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);
        Debug.Log($"→ Attempting to spawn {count} enemies in {room.roomObject.name}");

        for (int i = 0; i < count; i++)
        {
            // pick a random point inside the XZ box
            Vector3 randXZ = new Vector3(
                Random.Range(-half.x, half.x),
                0,
                Random.Range(-half.z, half.z)
            );
            Vector3 samplePos = center + randXZ;

            // sample the NavMesh with a generous vertical tolerance
            const float maxDist = 20f;
            if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
            {
                Debug.Log($"→ Instantiating enemy at {hit.position}");
                var e = Instantiate(enemyPrefab, hit.position, Quaternion.identity, dungeonRoot);
                if (!e.TryGetComponent<NavMeshAgent>(out _))
                    e.AddComponent<NavMeshAgent>();
            }
            else
            {
                Debug.Log($"X no NavMesh near {samplePos}");
            }
        }
    }
    */

    // Visualize the triangulation in the Scene view
    private void VisualizeTriangulation(List<DelaunayTriangulation.Triangle> triangles)
    {
        foreach (var triangle in triangles)
        {
            Debug.DrawLine(new Vector3(triangle.A.x, 0, triangle.A.y), new Vector3(triangle.B.x, 0, triangle.B.y), Color.cyan, 100f);
            Debug.DrawLine(new Vector3(triangle.B.x, 0, triangle.B.y), new Vector3(triangle.C.x, 0, triangle.C.y), Color.cyan, 100f);
            Debug.DrawLine(new Vector3(triangle.C.x, 0, triangle.C.y), new Vector3(triangle.A.x, 0, triangle.A.y), Color.cyan, 100f);
        }
    }

    void PlaceUnderRoot(GameObject go)
    {
        go.transform.SetParent(dungeonRoot, true);
    }

    /*
    void OnDrawGizmos()
    {
        // Draw door extension tiles in a distinct color (e.g., magenta)
        Gizmos.color = Color.magenta;
        foreach (RoomData room in placedRoomsData)
        {
            foreach (Transform door in room.doors)
            {
                List<Vector3> extensionTiles = DoorUtility.GetDoorExtensionTiles(door, settings.corridorYPosition);
                foreach (Vector3 tilePos in extensionTiles)
                {
                    // Draw a cube at the raw position
                    Gizmos.DrawCube(tilePos, Vector3.one * (settings.snapGridSize * 0.8f));
                }
            }
        }

        // Visualize corridor start and end CenterTiles
        if (settings != null && placedRoomsData != null && placedRoomsData.Count > 0)
        {
            HashSet<Edge> edges = new();
            List<Vector2> roomCenters = new();
            foreach (RoomData room in placedRoomsData)
            {
                BoxCollider col = room.roomObject.GetComponent<BoxCollider>();
                Vector2 center = new Vector2(room.position.x + col.center.x, room.position.z + col.center.z);
                roomCenters.Add(center);
            }

            // Recreate triangulation and MST for visual aid
            List<DelaunayTriangulation.Triangle> triangles = DelaunayTriangulation.BowyerWatson(roomCenters);
            edges = MSTUtility.CreateMinimumSpanningTree(MSTUtility.ExtractEdgesFromTriangles(triangles));

            foreach (Edge edge in edges)
            {
                RoomData startRoom = MapUtility.FindRoomAtPosition(edge.A, placedRoomsData);
                RoomData endRoom = MapUtility.FindRoomAtPosition(edge.B, placedRoomsData);
                if (startRoom == null || endRoom == null) continue;

                Transform startDoor, endDoor;
                Vector3 start = DoorUtility.GetBestDoorPosition(startRoom, endRoom.position, null, out startDoor, settings);
                Vector3 end = DoorUtility.GetBestDoorPosition(endRoom, startRoom.position, null, out endDoor, settings);

                // Draw start tile (green)
                Gizmos.color = Color.green;
                Gizmos.DrawCube(start + Vector3.up * 0.1f, Vector3.one * (settings.snapGridSize * 0.5f));

                // Draw end tile (red)
                Gizmos.color = Color.red;
                Gizmos.DrawCube(end + Vector3.up * 0.1f, Vector3.one * (settings.snapGridSize * 0.5f));

                // Draw line connecting them
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(start, end);
            }
        }

        // Existing Gizmos drawing for rooms and doors...
        Gizmos.color = Color.yellow;
        foreach (RoomData room in placedRoomsData)
        {
            BoxCollider col = room.roomObject.GetComponent<BoxCollider>();
            Vector3 center = room.position + col.center;
            Vector3 size = col.size + new Vector3(settings.roomPadding * 2, 0, settings.roomPadding * 2);
            Gizmos.DrawWireCube(center, size);
        }
    }
    */
}
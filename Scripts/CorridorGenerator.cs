using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    public static class CorridorGenerator
    {
        public static void GenerateCorridors
        (
            AStarGrid grid,
            HashSet<Edge> edges,
            List<RoomData> placedRoomsData,
            HashSet<Vector3> placedCorridorTiles,
            HashSet<Transform> connectedDoors,
            GeneratorSettings settings,
            Transform dungeonRoot
        )
        {
            foreach (Edge edge in edges)
            {
                RoomData startRoom = MapUtility.FindRoomAtPosition(edge.A, placedRoomsData);
                RoomData endRoom = MapUtility.FindRoomAtPosition(edge.B, placedRoomsData);
                if (startRoom == null || endRoom == null) continue;

                // Get doors + CenterTile positions
                Transform startDoor;
                Vector3 startCenter = DoorUtility.GetBestDoorPosition(startRoom, endRoom.position, grid, out startDoor, settings);
                Transform endDoor;
                Vector3 endCenter = DoorUtility.GetBestDoorPosition(endRoom, startRoom.position, grid, out endDoor, settings);

                connectedDoors.Add(startDoor);
                connectedDoors.Add(endDoor);

                // Removed snapping here and now center tile is being placed properly
                Vector3 pathStart = SnapToGrid(startCenter, settings.snapGridSize, startCenter.y);
                Vector3 pathEnd = SnapToGrid(endCenter, settings.snapGridSize, endCenter.y);

                // Create corridor group GameObject for hierarchy organization
                string corridorName = $"Corridor_{startRoom.roomObject.name}_to_{endRoom.roomObject.name}";

                GameObject corridorGroup = new GameObject(corridorName);
                corridorGroup.transform.SetParent(dungeonRoot, true); // NEW

                var path = AStar.FindPath(pathStart, pathEnd, grid);
                if (path == null || path.Count < 2)
                {
                    Debug.LogWarning($"No valid path between {pathStart} and {pathEnd}");
                    Debug.DrawLine(pathStart, pathEnd, Color.red, 10f);
                    continue;
                }
                
                CreateCorridorTileByTile(path, placedCorridorTiles, settings, corridorGroup.transform);
            }
        }

        private static void PlaceCorridorTile(Vector3 position, GeneratorSettings settings, Transform parent)
        {
            Vector3 snapped = position;
            if (parent.Find($"Tile_{snapped.x}_{snapped.z}") != null)
                return;

            GameObject tile = Object.Instantiate(settings.corridorPrefab, snapped, Quaternion.identity);
            
            tile.name = $"Tile_{snapped.x}_{snapped.z}";
            tile.layer = LayerMask.NameToLayer("Corridors");
            tile.transform.SetParent(parent);

            BoxCollider col = tile.GetComponent<BoxCollider>() ?? tile.AddComponent<BoxCollider>();
            col.size = new Vector3(settings.snapGridSize, 0, settings.snapGridSize);
            col.center = Vector3.zero;
        }

        private static void CreateCorridorTileByTile(List<Vector3> path, HashSet<Vector3> placedTiles, GeneratorSettings settings, Transform parent)
        {
            float tileSize = settings.snapGridSize;

            HashSet<Vector3> snappedPath = new HashSet<Vector3>();
            foreach (var point in path)
            {
                Vector3 snapped = SnapToGrid(point, tileSize, point.y);

                // Only place unique snapped tiles
                if (snappedPath.Add(snapped) && placedTiles.Add(snapped))
                {
                    PlaceCorridorTile(snapped, settings, parent);
                }
            }
        }

        private static Vector3 SnapToGrid(Vector3 pos, float gridSize, float y)
        {
            float snappedX = Mathf.Floor(pos.x / gridSize) * gridSize + gridSize * 0.5f;
            float snappedZ = Mathf.Floor(pos.z / gridSize) * gridSize + gridSize * 0.5f;
            return new Vector3(snappedX, y, snappedZ);
        }


        public static void PlaceWallsAroundCorridors(HashSet<Vector3> placedCorridorTiles, HashSet<Transform> connectedDoors, GeneratorSettings settings, Transform dungeonRoot)
        {
            Vector3[] directions = new Vector3[]
            {
                Vector3.forward * settings.snapGridSize,
                Vector3.back * settings.snapGridSize,
                Vector3.left * settings.snapGridSize,
                Vector3.right * settings.snapGridSize
            };

            float wallCheckSize = 0.2f;

            foreach (var tile in placedCorridorTiles)
            {
                foreach (var dir in directions)
                {
                    Vector3 neighborPos = tile + dir;

                    if (placedCorridorTiles.Contains(neighborPos))
                        continue;

                    Vector3 wallPos = tile + dir / 2f;
                    if (Physics.CheckBox(wallPos, new Vector3(wallCheckSize, settings.corridorHeight, wallCheckSize),
                        Quaternion.identity, settings.roomWallsMask | settings.roomFloorsMask))
                    {
                        continue;
                    }

                    float raycastHeight = 3f;
                    if (Physics.Raycast(wallPos + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hit, raycastHeight * 2f, settings.roomFloorsMask))
                        wallPos.y = hit.point.y;
                    else
                        wallPos.y = settings.corridorYPosition;

                    Quaternion rot = Quaternion.LookRotation(-dir.normalized, Vector3.up);
                    GameObject wall = Object.Instantiate(settings.wallPrefab, wallPos, rot);
                    wall.transform.SetParent(dungeonRoot, worldPositionStays: true);
                }
            }
        }
    }
}

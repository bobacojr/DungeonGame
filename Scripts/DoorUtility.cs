using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    public static class DoorUtility
    {
        public static Vector3 GetBestDoorPosition
        (
            RoomData room, 
            Vector3 targetDirection, 
            AStarGrid grid, 
            out Transform bestDoor,
            GeneratorSettings settings
        )
        {
            bestDoor = null;

            if (room.doors.Count == 0)
            {
                BoxCollider collider = room.roomObject.GetComponent<BoxCollider>();
                return room.position + (collider != null ? collider.center : Vector3.zero);
            }

            float bestScore = float.MinValue;

            foreach (Transform door in room.doors)
            {
                Vector3 doorDir = GetDoorOutwardDirection(door);
                Vector3 toTarget = (targetDirection - door.position).normalized;
                float alignmentScore = Vector3.Dot(doorDir, toTarget) * 2f - Vector3.Distance(door.position, targetDirection);

                if (alignmentScore > bestScore)
                {
                    bestScore = alignmentScore;
                    bestDoor = door;
                }
            }

            if (bestDoor == null)
                return room.position;

            return GetCenterTilePosition(bestDoor);
        }

        public static Vector3 GetDoorOutwardDirection(Transform door)
        {
            string doorName = door.name.ToLower();
            if (doorName.Contains("up"))
                return Vector3.forward;  // +z
            if (doorName.Contains("down"))
                return Vector3.back;     // -z
            if (doorName.Contains("right"))
                return Vector3.right;    // +x
            if (doorName.Contains("left"))
                return Vector3.left;     // -x

            return door.forward; // fallback if name doesn't match
        }

        public static Vector3 GetDoorConnectionPosition(Transform door, AStarGrid grid, GeneratorSettings settings)
        {
            Transform extension = door.Find("Extension");

            if (extension != null)
            {
                // 1. Look explicitly for a tile named "CenterTile"
                Transform centerTile = extension.Find("CenterTile");
                if (centerTile != null && centerTile.gameObject.activeSelf)
                {
                    Vector3 pos = centerTile.position;
                    return new Vector3(
                        pos.x,
                        pos.y,
                        pos.z
                    );
                }

                // 2. Fallback: try to find any active tile if no "CenterTile" is found
                foreach (Transform child in extension)
                {
                    if (child.gameObject.activeSelf && child.name.Contains("Tile"))
                    {
                        Vector3 pos = child.position;
                        return new Vector3(
                            Mathf.Round(pos.x / settings.snapGridSize) * settings.snapGridSize,
                            pos.y,
                            Mathf.Round(pos.z / settings.snapGridSize) * settings.snapGridSize
                        );
                    }
                }
            }

            // 3. Fallback: snap the door's own position
            Vector3 doorPos = door.position;
            return new Vector3(
                Mathf.Round(doorPos.x / settings.snapGridSize) * settings.snapGridSize,
                doorPos.y,
                Mathf.Round(doorPos.z / settings.snapGridSize) * settings.snapGridSize
            );
        }

        public static List<Vector3> GetDoorExtensionTiles(Transform door, float corridorYPosition)
        {
            List<Vector3> tiles = new List<Vector3>();
            Transform extension = door.Find("Extension");
            if (extension != null)
            {
                foreach (Transform child in extension)
                {
                    // Only add children whose name contains "Tile"
                    if (child.gameObject.activeSelf && child.name.Contains("Tile"))
                    {
                        Vector3 tilePos = child.position;
                        tilePos.y = corridorYPosition;
                        tiles.Add(tilePos);
                    }
                }
            }
            return tiles;
        }

        public static Vector3 GetCenterTilePosition(Transform door)
        {
            if (door == null)
            {
                Debug.LogError("GetCenterTilePosition received null door.");
                return Vector3.zero;
            }

            Transform extension = door.Find("Extension");
            if (extension == null)
            {
                Debug.LogWarning($"🚪 Door '{door.name}' has no 'Extension' child.");
                return door.position;
            }

            Transform centerTile = extension.Find("CenterTile");
            if (centerTile == null)
            {
                Debug.LogWarning($"'CenterTile' not found under extension of '{door.name}'.");
                return door.position;
            }
            Debug.Log($"CenterTile Position: {centerTile.position}");
            return centerTile.position; // no snapping
        }

        private static Vector3 SnapToGrid(Vector3 pos, GeneratorSettings settings)
        {
            return new Vector3(
                Mathf.Round(pos.x / settings.snapGridSize) * settings.snapGridSize,
                settings.corridorYPosition,
                Mathf.Round(pos.z / settings.snapGridSize) * settings.snapGridSize
            );
        }

        public static void FinalizeDoors(List<RoomData> placedRoomsData, HashSet<Transform> connectedDoors)
        {
            foreach (RoomData room in placedRoomsData)
            {
                Transform roomRoot = room.roomObject.transform;

                foreach (Transform door in room.doors)
                {
                    string doorName = door.name; // Example: "Up Door"
                    string wallName = doorName.Replace("Door", "Wall"); // -> "Up Wall"

                    Transform wall = roomRoot.Find(wallName);

                    if (connectedDoors.Contains(door))
                    {
                        door.gameObject.SetActive(true); // Show connected door
                        if (wall != null) wall.gameObject.SetActive(false); // Hide matching wall
                    }
                    else
                    {
                        door.gameObject.SetActive(false); // Hide unused door
                        if (wall != null) wall.gameObject.SetActive(true); // Show wall
                    }
                }
            }
        }

        public static Vector3 RoundDirectionToCardinal(Vector3 direction)
        {
            // Compare absolute x and z components; zero out the smaller one.
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                return new Vector3(Mathf.Sign(direction.x), 0, 0);
            else
                return new Vector3(0, 0, Mathf.Sign(direction.z));
        }
    }
}

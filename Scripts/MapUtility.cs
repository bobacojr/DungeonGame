using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    public static class MapUtility
    {
        public static bool TryPlacingRoom
        (
            GameObject room, 
            List<RoomData> placedRoomsData, 
            Vector2 mapSize, float roomPadding, 
            int maxPlacementAttempts, 
            GeneratorSettings settings,
            out RoomData placedRoom
        ) 
        {
            placedRoom = null;

            BoxCollider roomCollider = room.GetComponent<BoxCollider>();
            Vector3 baseSize = roomCollider.size;
            Vector3 colliderOffset = roomCollider.center;

            Vector3 roomSize = baseSize;

            int attempts = 0;
            bool validPosition = false;
            Vector3 position = Vector3.zero;

            float snapGridSize = settings.snapGridSize;

            while (!validPosition && attempts < maxPlacementAttempts)
            {
                attempts++;

                float randomX = Random.Range(-mapSize.x / 2, mapSize.x / 2);
                float randomY = Random.Range(-mapSize.y / 2, mapSize.y / 2);

                float snappedX = Mathf.Round(randomX / snapGridSize) * snapGridSize;
                float snappedY = Mathf.Round(randomY / snapGridSize) * snapGridSize;
                position = new Vector3(snappedX, 0, snappedY);

                Vector3 colliderWorldPos = position + colliderOffset;

                // Check if room fits within the map bounds (with padding)
                Vector3 paddedHalfSize = (roomSize + new Vector3(roomPadding * 2, 0, roomPadding * 2)) / 2f;

                if (Mathf.Abs(colliderWorldPos.x) + paddedHalfSize.x > mapSize.x / 2f ||
                    Mathf.Abs(colliderWorldPos.z) + paddedHalfSize.z > mapSize.y / 2f)
                {
                    continue; // Skip, room would be out of bounds
                }

                // Check overlap with all previously placed rooms
                validPosition = true;
                foreach (RoomData currentPlacedRoom in placedRoomsData)
                {
                    BoxCollider placedCollider = currentPlacedRoom.roomObject.GetComponent<BoxCollider>();
                    Vector3 placedColliderPos = currentPlacedRoom.position + placedCollider.center;
                    Vector3 placedSize = placedCollider.size;

                    if (CheckOverlap(colliderWorldPos, roomSize, placedColliderPos, placedSize, settings))
                    {
                        validPosition = false;
                        break;
                    }
                }
            }

            // If position is valid, instantiate and track it
            if (validPosition)
            {
                GameObject newRoom = Object.Instantiate(room, position, Quaternion.identity);

                BoxCollider newCollider = newRoom.GetComponent<BoxCollider>();
                RoomData roomData = new RoomData
                {
                    position = position,
                    size = newCollider.size,
                    roomObject = newRoom
                };

                roomData.FindDoors();
                var spawners = newRoom.GetComponentsInChildren<FurnitureSpawner>();
                foreach (var spawner in spawners)
                {
                    spawner.Spawn();
                }
                placedRoomsData.Add(roomData);
                placedRoom = roomData;
                return true;
            }
            return false;
        }

        public static RoomData FindRoomAtPosition(Vector2 position, List<RoomData> placedRoomsData)
        {
            foreach (RoomData room in placedRoomsData)
            {
                BoxCollider collider = room.roomObject.GetComponent<BoxCollider>();
                Bounds bounds = new Bounds(
                    room.position + collider.center, 
                    collider.size
                );
                
                // Check if position is within room bounds
                if (bounds.Contains(new Vector3(position.x, 0, position.y)))
                {
                    return room;
                }
            }
            return null;
        }

        public static bool CheckOverlap(Vector3 pos1, Vector3 size1, Vector3 pos2, Vector3 size2, GeneratorSettings settings)
        {
            // Add padding directly to the bounds during the check
            Bounds bounds1 = new Bounds(pos1, size1 + new Vector3(settings.roomPadding * 2, 0, settings.roomPadding * 2));
            Bounds bounds2 = new Bounds(pos2, size2 + new Vector3(settings.roomPadding * 2, 0, settings.roomPadding * 2));
            return bounds1.Intersects(bounds2);
        }
    }
}
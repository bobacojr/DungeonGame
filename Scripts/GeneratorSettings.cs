using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    [CreateAssetMenu(fileName = "GeneratorSettings", menuName = "Dungeon Generation/Generator Settings")]
    public class GeneratorSettings : ScriptableObject
    {
        [Header("Pathfinding Settings")]
        public GameObject corridorPrefab;
        public float corridorWidth = 1f;
        public float corridorHeight = 1f;
        public LayerMask corridorObstacleMask;
        public float corridorYPosition = -1.4f;

        [Header("Snapping Settings")]
        public float snapGridSize = 1f;
        public bool enableSnapping = true;

        [Header("Wall Settings")]
        public GameObject wallPrefab;
        public float wallOffset = 1f;

        [Header("Collider Settings")]
        public LayerMask roomWallsMask;
        public LayerMask roomFloorsMask;

        [Header("General Settings")]
        public GameObject playerPrefab;
        public GameObject spawnRoomPrefab;
        public List<GameObject> roomPrefabs;
        public int numberOfRooms = 10;
        public Vector2 mapSize = new(60, 60);
        public float roomPadding = 1f;
        public int maxPlacementAttempts = 100;
    }
}

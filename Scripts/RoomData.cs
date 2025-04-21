using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    public class RoomData
    {
        public Vector3 position; // Position of the room
        public Vector3 size; // Size of the room
        public GameObject roomObject; // Represents the entire room
        public List<Transform> doors = new(); // Contains each door object in the room (4 doors)

        public void FindDoors() // Find the doors and add them to the list
        {
            doors.Clear();
            foreach (Transform child in roomObject.transform)
            {
                if (child.CompareTag("Door"))
                {
                    doors.Add(child);
                }
            }
        }
    }
}
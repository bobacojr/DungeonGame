using System.Collections.Generic;
using UnityEngine;

public class FurnitureSpawner : MonoBehaviour
{
    public List<GameObject> furnitureOptions;
    public float spawnChance = 1f;

    public void Spawn()
    {
        if (furnitureOptions == null || furnitureOptions.Count == 0)
        {
            return;
        }

        if (Random.value <= spawnChance)
        {
            GameObject selected = furnitureOptions[Random.Range(0, furnitureOptions.Count)];
            Instantiate(selected, transform.position, transform.rotation, transform);
        }
    }
}

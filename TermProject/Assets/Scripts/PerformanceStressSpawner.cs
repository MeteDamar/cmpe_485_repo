using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceStressSpawner : MonoBehaviour
{
    [Header("Stress Objects")]
    public GameObject[] objectPrefabs;

    [Header("Spawn Settings")]
    public int objectsPerKeycard = 1000;
    public int objectsPerFrame = 100;
    public float spawnHeight = 8f;
    public float roomSpawnAreaSize = 8f;

    public void SpawnStressObjectsInRoom(RoomNode room)
    {
        if (room == null)
        {
            Debug.LogWarning("Room is null.");
            return;
        }

        StartCoroutine(SpawnObjectsCoroutine(room.transform.position, objectsPerKeycard));
    }

    private IEnumerator SpawnObjectsCoroutine(Vector3 roomCenter, int totalCount)
    {
        if (objectPrefabs == null || objectPrefabs.Length == 0)
        {
            Debug.LogWarning("No stress object prefabs assigned.");
            yield break;
        }

        int spawned = 0;

        while (spawned < totalCount)
        {
            int batchCount = Mathf.Min(objectsPerFrame, totalCount - spawned);

            for (int i = 0; i < batchCount; i++)
            {
                SpawnOneObject(roomCenter);
                spawned++;
            }

            yield return null; // wait 1 frame after each batch
        }

        Debug.Log("Spawned total stress objects: " + spawned);
    }

    private void SpawnOneObject(Vector3 roomCenter)
    {
        GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];

        Vector3 spawnPosition = roomCenter + new Vector3(
            Random.Range(-roomSpawnAreaSize, roomSpawnAreaSize),
            spawnHeight,
            Random.Range(-roomSpawnAreaSize, roomSpawnAreaSize)
        );

        Instantiate(prefab, spawnPosition, Random.rotation);
    }
}
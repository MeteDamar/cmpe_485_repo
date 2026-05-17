using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public int totalKeycardsNeeded = 3;
    public int collectedKeycards = 0;

    [Header("Performance Stress Test")]
    public PerformanceStressSpawner stressSpawner;

    public bool HasAllKeycards()
    {
        return collectedKeycards >= totalKeycardsNeeded;
    }

    public void CollectKeycard(RoomNode collectedRoom)
    {
        collectedKeycards++;

        Debug.Log("Keycard collected: " + collectedKeycards + "/" + totalKeycardsNeeded);

        if (stressSpawner != null && collectedRoom != null)
        {
            stressSpawner.SpawnStressObjectsInRoom(collectedRoom);
        }

        if (HasAllKeycards())
        {
            Debug.Log("All keycards collected. Find the exit!");
        }
    }
}
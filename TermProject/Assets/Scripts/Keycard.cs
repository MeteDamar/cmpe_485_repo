using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Keycard : MonoBehaviour
{
    public ObjectiveManager objectiveManager;
    public RoomNode room;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() != null || other.GetComponent<PlayerHiding>() != null)
        {
            if (objectiveManager == null)
            {
                Debug.LogError("ObjectiveManager is not assigned on " + gameObject.name);
                return;
            }

            objectiveManager.CollectKeycard(room);
            Destroy(gameObject);
        }
    }
}
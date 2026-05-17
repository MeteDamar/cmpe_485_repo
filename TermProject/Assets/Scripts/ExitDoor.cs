using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    public ObjectiveManager objectiveManager;

    private void Awake()
    {
        if (objectiveManager == null)
            objectiveManager = FindObjectOfType<ObjectiveManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() == null && other.GetComponent<PlayerHiding>() == null)
            return;

        if (objectiveManager == null)
            objectiveManager = FindObjectOfType<ObjectiveManager>();

        if (objectiveManager != null && objectiveManager.HasAllKeycards())
        {
            Debug.Log("YOU WIN!");

            GameManager gameManager = FindObjectOfType<GameManager>();

            if (gameManager != null)
            {
                gameManager.WinGame();
            }
            else
            {
                Debug.LogError("GameManager is not assigned or found for " + gameObject.name);
            }
        }
        else
        {
            Debug.Log("You need all 3 keycards before escaping.");
        }
    }
}

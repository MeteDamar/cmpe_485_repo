using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorCollider : MonoBehaviour
{
    public GameObject winText;
    public GameObject doorObject;

    private bool gameEnded = false;
    

    private void OnCollisionEnter(Collision collision)
    {
        if (gameEnded) return;

        if (collision.gameObject.CompareTag("Key"))
        {
            gameEnded = true;

            Debug.Log("YOU WIN!");

            if (winText != null)
            {
                winText.SetActive(true);
            }

            if (doorObject != null)
            {
                doorObject.SetActive(false);
            }

            Time.timeScale = 0f;
        }
    }
}

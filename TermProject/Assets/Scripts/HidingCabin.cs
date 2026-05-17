using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidingCabin : MonoBehaviour
{
    public RoomNode room;

    [Header("Positions")]
    public Transform hidePosition;
    public Transform exitPosition;

    public bool PlayerIsHidingHere = false;

    private PlayerHiding nearbyPlayer;

    public void SetPlayerHidingHere(bool isHidingHere)
    {
        PlayerIsHidingHere = isHidingHere;
    }

    void Update()
    {
        if (nearbyPlayer == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (nearbyPlayer.IsHiding)
            {
                if (nearbyPlayer.CurrentCabin == this)
                {
                    nearbyPlayer.ExitCabin();
                }
            }
            else
            {
                nearbyPlayer.EnterCabin(this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHiding player = other.GetComponent<PlayerHiding>();

        if (player != null)
        {
            nearbyPlayer = player;
            Debug.Log("Press E to hide in: " + gameObject.name);
        }

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();

        if (enemy != null && PlayerIsHidingHere)
        {
            Debug.Log("Enemy found player hiding in cabin trigger!");

            GameManager gameManager = FindObjectOfType<GameManager>();

            if (gameManager != null)
                gameManager.LoseGame();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHiding player = other.GetComponent<PlayerHiding>();

        if (player != null && nearbyPlayer == player)
        {
            if (!player.IsHiding)
            {
                nearbyPlayer = null;
            }
        }
    }
}

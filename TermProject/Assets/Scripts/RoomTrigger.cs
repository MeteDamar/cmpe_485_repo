using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RoomTrigger : MonoBehaviour
{
    public RoomNode room;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHiding player = other.GetComponent<PlayerHiding>();

        if (player != null)
        {
            player.SetCurrentRoom(room);
        }
    }
}

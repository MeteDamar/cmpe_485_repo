using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    public bool IsHiding { get; private set; }
    public RoomNode CurrentRoom { get; private set; }
    public HidingCabin CurrentCabin { get; private set; }

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void EnterCabin(HidingCabin cabin)
    {
        if (cabin == null) return;

        if (CurrentCabin != null)
            CurrentCabin.SetPlayerHidingHere(false);

        IsHiding = true;
        CurrentCabin = cabin;
        CurrentRoom = cabin.room;
        CurrentCabin.SetPlayerHidingHere(true);

        if (controller != null)
            controller.enabled = false;

        transform.position = cabin.hidePosition.position;
        transform.rotation = cabin.hidePosition.rotation;

        if (controller != null)
            controller.enabled = true;

        Debug.Log("Player entered cabin: " + cabin.name);
    }

    public void ExitCabin()
    {
        if (!IsHiding || CurrentCabin == null) return;

        HidingCabin cabinToExit = CurrentCabin;

        if (controller != null)
            controller.enabled = false;

        transform.position = cabinToExit.exitPosition.position;
        transform.rotation = cabinToExit.exitPosition.rotation;

        if (controller != null)
            controller.enabled = true;

        IsHiding = false;
        cabinToExit.SetPlayerHidingHere(false);
        CurrentCabin = null;

        Debug.Log("Player exited cabin.");
    }

    public void SetCurrentRoom(RoomNode room)
    {
        if (!IsHiding)
        {
            CurrentRoom = room;
        }
    }
}

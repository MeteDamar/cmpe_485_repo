using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    public float distance = 5f;   // how far behind
    public float height = 3f;     // how high
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        // Desired position behind the player
        Vector3 desiredPosition = player.position 
                                - player.forward * distance 
                                + Vector3.up * height;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Always look at player
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}
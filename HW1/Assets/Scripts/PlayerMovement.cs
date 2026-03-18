using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;

    public float moveSpeed = 40f;
    public float turnSpeed = 100f;

    private float turnInput;
    private float moveInput;
    private float fixedY;
    private float currentYRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        fixedY = transform.position.y;
        currentYRotation = transform.eulerAngles.y;

        rb.constraints =
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        turnInput = Input.GetAxis("Horizontal");
        moveInput = Input.GetAxis("Vertical");
    }

    void FixedUpdate()
    {
        currentYRotation += turnInput * turnSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.Euler(0f, currentYRotation, 0f));

        Vector3 movement = transform.forward * moveInput * moveSpeed;
        rb.velocity = new Vector3(movement.x, 0f, movement.z);

        Vector3 pos = rb.position;
        pos.y = fixedY;
        rb.position = pos;

        rb.angularVelocity = Vector3.zero;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public Transform playerCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation;

    private PlayerHiding playerHiding;
    private GameManager gameManager;

    public bool IsMoving { get; private set; }
    public bool IsSprinting { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerHiding = GetComponent<PlayerHiding>();
        gameManager = FindObjectOfType<GameManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (gameManager != null && gameManager.GameEnded)
        {
            IsMoving = false;
            IsSprinting = false;
            return;
        }

        Look();
    
        if (playerHiding != null && playerHiding.IsHiding)
        {
            IsMoving = false;
            IsSprinting = false;
            return;
        }
    
        Move();
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        IsMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;
        IsSprinting = Input.GetKey(KeyCode.LeftShift) && IsMoving;

        float speed = IsSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}

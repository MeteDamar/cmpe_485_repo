using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    public EnemyAI enemy;
    public float sprintNoiseCooldown = 2f;

    private float timer;
    private PlayerMovement movement;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();

        // Allows first sprint noise immediately.
        timer = sprintNoiseCooldown;
    }

    void Update()
    {
        if (enemy == null)
        {
            Debug.LogWarning("NoiseEmitter: Enemy is not assigned.");
            return;
        }

        if (movement == null)
        {
            Debug.LogWarning("NoiseEmitter: PlayerMovement not found.");
            return;
        }

        timer += Time.deltaTime;

        if (movement.IsSprinting && timer >= sprintNoiseCooldown)
        {
            enemy.HearNoise(transform.position);
            timer = 0f;

            Debug.Log("Noise emitted at: " + transform.position);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    public float activeTime = 2f;
    public float inactiveTime = 2f;

    public GameObject loseText;

    private Collider trapCollider;
    private Renderer trapRenderer;

    private bool gameEnded = false;
    

    void Start()
    {
        trapCollider = GetComponent<Collider>();
        trapRenderer = GetComponent<Renderer>();

        StartCoroutine(TrapCycle());
    }

    IEnumerator TrapCycle()
    {
        while (true)
        {
            // Trap ON
            trapCollider.enabled = true;
            if (trapRenderer != null)
                trapRenderer.material.color = Color.red;

            yield return new WaitForSeconds(activeTime);

            // Trap OFF
            trapCollider.enabled = false;
            if (trapRenderer != null)
                trapRenderer.material.color = Color.gray;

            yield return new WaitForSeconds(inactiveTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameEnded) return;

        if (collision.gameObject.CompareTag("Player") && trapCollider.enabled)
        {
            gameEnded = true;

            Debug.Log("YOU DIED");

            if (loseText != null)
                loseText.SetActive(true);

            Time.timeScale = 0f;
        }
    }
}
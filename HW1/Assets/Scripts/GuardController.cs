using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardController : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    public float waitTime = 1f;

    public GameObject loseText;

    private bool gameEnded = false;

    void Start()
    {
        StartCoroutine(Patrol());
    }

    IEnumerator Patrol()
    {
        while (true)
        {
            // Move A → B
            yield return StartCoroutine(MoveTo(pointB));

            yield return new WaitForSeconds(waitTime);

            // Move B → A
            yield return StartCoroutine(MoveTo(pointA));

            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator MoveTo(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > 0.1f)
        {
            if (gameEnded) yield break;

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

            // Face movement direction
            Vector3 dir = target.position - transform.position;
            if (dir != Vector3.zero)
                transform.forward = dir;

            yield return null; // wait for next frame
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameEnded) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            gameEnded = true;

            if (loseText != null)
                loseText.SetActive(true);

            Time.timeScale = 0f;
        }
    }
}
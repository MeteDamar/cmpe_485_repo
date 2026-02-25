using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMover : MonoBehaviour
{
    [SerializeField]
    private Vector3 direction;
    private Vector3 startPos;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        direction = new Vector3(x, 0, z);
        
        rb.AddForce(direction);
    }
}

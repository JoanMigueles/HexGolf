using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody rb;
    private Vector3 reposition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetReposition();
    }

    private void Update()
    {
        if (transform.position.y < -10f) {
            Reposition();
        }
    }

    public void SetReposition()
    {
        reposition = transform.position;
    }

    public void Reposition()
    {
        rb.velocity = Vector3.zero;
        transform.position = reposition + new Vector3(0f, 0.6f, 0f);
    }

    public void SetVelocity(Vector3 velocity)
    {
        rb.velocity = velocity;
    }

    public bool IsMoving()
    {
        if (rb.velocity.magnitude > 0.05) {
            return true;
        }
        return false;
    }
}

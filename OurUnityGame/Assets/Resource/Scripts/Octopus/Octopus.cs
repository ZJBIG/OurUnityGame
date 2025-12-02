using System.Collections;
using UnityEngine;

public class Octopus : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sp;
    public float speed;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sp = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        sp.flipX = rb.velocity.x > 0;
        Debug.Log(rb.velocity);
    }
}

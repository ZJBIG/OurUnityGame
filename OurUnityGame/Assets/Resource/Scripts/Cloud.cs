using System;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator ani;
    private float speed => UnityEngine.Random.Range(-0.5f, 0.5f);
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        ani = GetComponent<Animator>();
        //初始化云(向左向右飘)
        rb.velocity = Vector2.right * speed;
        sr.flipX = rb.velocity.x >= 0;
        if (ani)
            ani.speed *= Math.Abs(speed);
        Destroy(gameObject, 300);
    }
}

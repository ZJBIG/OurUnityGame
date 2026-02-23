using System;
using System.Collections;
using UnityEngine;

public enum Status { Idle, Tracing, Hurt }
public class Octopus : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sp;
    Transform Player;
    Action action;
    public Status status;

    public float Speed;
    public float TracingDistance;

    [Header("攻击相关")]

    public float AttackCD;
    public float AttackDistance;
    public float BeatImpulse;
    public float StunTime;
    private bool CanAttack = true;


    private Vector2 Direction;
    [HideInInspector] public bool isHurt;
    void Start()
    {
        rb = transform.parent.GetComponent<Rigidbody2D>();
        sp = GetComponent<SpriteRenderer>();
        Player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        UpdateDirection();
        UpdateStatus();
        action.Invoke();
    }
    private void FixedUpdate()
    {
        if (Vector2.Distance(Player.position, transform.position) <= AttackDistance && CanAttack && !isHurt)
            StartCoroutine(Attack());
    }

    private void UpdateStatus()
    {
        if (isHurt)
            status = Status.Hurt;
        else if (Vector2.Distance(Player.position, transform.position) <= TracingDistance)
            status = Status.Tracing;
        else
            status = Status.Idle;
        action = status switch
        {
            Status.Idle => Idle,
            Status.Tracing => Tracing,
            _ => Hurt
        };
    }

    private void Idle()
    {

    }
    private void Tracing() => rb.velocity = (Player.position - transform.position).normalized * Speed * new Vector2(1, 0);
    private void Hurt() => rb.velocity = Vector2.zero;
    private void UpdateDirection()
    {
        if (Math.Abs(rb.velocity.x) >= 0.01f)
            Direction = rb.velocity.normalized * new Vector2(1, 0);
        sp.flipX = Direction.x == 1;
    }
    IEnumerator Attack()
    {
        Player.GetComponent<PlayerCtrl>().LockMoveTime = StunTime;//锁定玩家移动
        Player.GetComponent<Rigidbody2D>().AddForce(Direction * BeatImpulse, ForceMode2D.Impulse);
        CanAttack = false;
        yield return new WaitForSecondsRealtime(AttackCD);
        CanAttack = true;
    }
    IEnumerator RandomMove()
    {
        yield return null;
    }
}

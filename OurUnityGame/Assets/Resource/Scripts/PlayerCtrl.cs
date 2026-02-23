using System.Collections;
using UnityEngine;

public class PlayerCtrl : MonoBehaviour
{
    private readonly float erf = 0.01f;
    [Header("冲刺/蹬墙后锁定水平移动时间")]
    public float LockHorizontalMoveTime;
    [Header("普通跳跃参数")]
    public float JumpImpulse;
    public float MaxJumpTime; private float CurJumpTime;
    public float WolfTime; private float EscapeGroundTime;
    [Header("基本设置")]
    public float MoveSpeed;
    public float DashImpulse;
    public float DashCD;
    public Vector2 WallSlideForce;
    public Vector2 WallJumpImpulse;
    [HideInInspector] public Vector2 Direction;
    [HideInInspector] public bool inSky, canDash = true;
    [HideInInspector] public bool onDashing;
    [HideInInspector] public float LockMoveTime;
    Rigidbody2D rb;
    Collider2D Collider;

    Vector2 RightColliderOffset = new Vector2(-0.0612676f, -0.3259298f);
    Vector2 LeftColliderOffset = new Vector2(0.05240458f, -0.3259298f);
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider = GetComponent<Collider2D>();
    }
    private void FixedUpdate()
    {
        if (WallDetect() && inSky && !(rb.velocity.y >= erf))
            rb.AddForce(WallSlideForce);
    }
    void Update()
    {
        UpdateFaceDirection();

        if (Direction == Vector2.right)
            Collider.offset = RightColliderOffset;
        if (Direction == Vector2.left)
            Collider.offset = LeftColliderOffset;

        LockMoveTime -= Time.deltaTime;
        if (GroundDetect() && LockMoveTime <= 0f)
        {
            CurJumpTime = 0f;
            inSky = false;
            EscapeGroundTime = 0f;
        }
        if (!GroundDetect())
        {
            EscapeGroundTime += Time.deltaTime;
            if (EscapeGroundTime >= WolfTime)
                inSky = true;
        }

        HorizontalMove();
        Jump();
        StartCoroutine(WallJump());
        StartCoroutine(Dash());
    }
    private void HorizontalMove()
    {
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) >= erf && LockMoveTime <= 0f)
            rb.velocity = new Vector2(Input.GetAxisRaw("Horizontal") * MoveSpeed, rb.velocity.y);
    }
    private void UpdateFaceDirection()
    {
        if (Mathf.Abs(rb.velocity.x) >= erf)
            Direction = new Vector2(rb.velocity.x, 0).normalized;
    }
    private void Jump()
    {
        // 检查是否在重力反转模式，如果是则跳过跳跃
        PlayerAbilities abilities = GetComponent<PlayerAbilities>();
        if ((abilities != null && abilities.IsInGravityReverseMode()) || LockMoveTime > 0)
            return; // 在重力反转模式下不执行跳跃
        if (Input.GetKey(KeyCode.Space) && CurJumpTime <= MaxJumpTime && !inSky)
        {
            rb.AddForce(Vector2.up * JumpImpulse, ForceMode2D.Impulse);
            CurJumpTime += Time.deltaTime;
        }
        if (!Input.GetKey(KeyCode.Space) && CurJumpTime >= erf)
            inSky = true;
    }
    private IEnumerator Dash()
    {
        if (!canDash || LockMoveTime > 0) yield break;
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            onDashing = true;
            Vector2 dir = rb.velocity.normalized;
            if (dir.magnitude <= erf) dir = Direction;
            rb.AddForce(new Vector2(dir.x, dir.y / 2.0f) * DashImpulse, ForceMode2D.Impulse);
            canDash = false;

            LockMoveTime = LockHorizontalMoveTime;

            yield return new WaitForSecondsRealtime(LockHorizontalMoveTime);
            onDashing = false;

            yield return new WaitForSecondsRealtime(DashCD - LockHorizontalMoveTime);
            while (!GroundDetect())
                yield return null;
            canDash = true;
        }
    }
    private IEnumerator WallJump()
    {
        if (LockMoveTime > 0) yield break;
        if (Input.GetKeyDown(KeyCode.Space) && inSky && WallDetect())
        {
            rb.AddForce(new Vector2(-WallJumpImpulse.x * Direction.x, WallJumpImpulse.y), ForceMode2D.Impulse);
            LockMoveTime = LockHorizontalMoveTime;
        }
    }
    public bool GroundDetect()
    {
        Ray2D ray1 = new Ray2D(transform.position + Vector3.left * 0.2f, Vector2.down);
        Ray2D ray2 = new Ray2D(transform.position + Vector3.right * 0.2f, Vector2.down);
        var hits1 = Physics2D.RaycastAll(ray1.origin, ray1.direction, 0.35f);
        var hits2 = Physics2D.RaycastAll(ray2.origin, ray2.direction, 0.35f);

        Debug.DrawLine(ray1.origin, ray1.origin + (Vector2)ray1.direction * 0.35f, Color.red);
        Debug.DrawLine(ray2.origin, ray2.origin + (Vector2)ray2.direction * 0.35f, Color.red);

        foreach (var hit in hits1)
            if (hit.transform.CompareTag("Ground"))
                return true;
        foreach (var hit in hits2)
            if (hit.transform.CompareTag("Ground"))
                return true;
        return false;
    }
    public bool WallDetect()
    {
        Ray2D ray1 = new Ray2D(transform.position + Vector3.up * 0.2f, Direction);
        Ray2D ray2 = new Ray2D(transform.position + Vector3.down * 0.2f, Direction);
        var hits1 = Physics2D.RaycastAll(ray1.origin, ray1.direction, 0.1f);
        var hits2 = Physics2D.RaycastAll(ray2.origin, ray2.direction, 0.1f);

        Debug.DrawLine(ray1.origin, ray1.origin + (Vector2)ray1.direction * 0.1f, Color.yellow);
        Debug.DrawLine(ray2.origin, ray2.origin + (Vector2)ray2.direction * 0.1f, Color.yellow);

        foreach (var hit in hits1)
            if (hit.transform.CompareTag("Wall"))
                return true;
        foreach (var hit in hits2)
            if (hit.transform.CompareTag("Wall"))
                return true;
        return false;
    }
}

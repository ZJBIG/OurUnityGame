using UnityEngine;
using System.Collections;

public class ProjectileBehavior : MonoBehaviour
{
    public float lifespan = 3f;
    private Rigidbody2D rb;
    public int maxBounces = 3;
    public int bounceCount = 0;

    // 碰撞状态控制 - 现在基于反弹次数
    public bool canCollideWithPlayer = false;
    public int requiredBouncesForPickup = 1; // 需要至少反弹几次才能接取

    // 物理材质相关
    public PhysicsMaterial2D bounceMaterial;
    private Vector2 lastVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 确保刚体设置正确
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // 应用物理材质
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                if (bounceMaterial == null)
                {
                    bounceMaterial = new PhysicsMaterial2D("BounceMaterial");
                    bounceMaterial.friction = 0.1f;
                    bounceMaterial.bounciness = 0.8f;
                }
                collider.sharedMaterial = bounceMaterial;

                // 新增：初始时忽略玩家碰撞（双重保护）
                StartCoroutine(InitialCollisionIgnore());
            }
        }

        // 初始状态：不可与玩家碰撞（需要反弹后才能接取）
        canCollideWithPlayer = false;

        // 生命周期管理
        Destroy(gameObject, lifespan);
    }

    // 新增：初始碰撞忽略协程
    private IEnumerator InitialCollisionIgnore()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            Collider2D projectileCollider = GetComponent<Collider2D>();

            if (playerCollider != null && projectileCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, projectileCollider, true);

                // 短暂延迟后恢复碰撞
                yield return new WaitForSeconds(0.3f);

                Physics2D.IgnoreCollision(playerCollider, projectileCollider, false);
                Debug.Log("ProjectileBehavior: 恢复与玩家的碰撞");
            }
        }
    }

    void Update()
    {
        // 保存上一帧的速度（用于反弹计算）
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }

        // 可视化调试：根据碰撞状态改变投射物颜色
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 不可碰撞：红色；可碰撞：绿色；已反弹但还不够：黄色
            if (canCollideWithPlayer)
            {
                spriteRenderer.color = Color.green;
            }
            else if (bounceCount > 0)
            {
                spriteRenderer.color = Color.yellow; // 已反弹但次数还不够
            }
            else
            {
                spriteRenderer.color = Color.red; // 尚未反弹
            }
        }

        // 防止速度过小导致粘墙
        if (rb != null && rb.velocity.magnitude < 0.5f)
        {
            Debug.Log("速度过小，销毁投射物");
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        string otherLayer = LayerMask.LayerToName(collision.gameObject.layer);
        Debug.Log($"投射物碰撞检测 - 对象: {collision.gameObject.name}, 标签: {collision.gameObject.tag}, 图层: {otherLayer}");

        // 1. 墙壁/地面碰撞处理
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log($"投射物碰撞墙壁/地面（反弹次数: {bounceCount + 1}/{maxBounces}）");

            // 执行物理反弹
            BounceOffWall(collision);
            bounceCount++;

            // 检查是否达到可接取的条件
            UpdatePickupEligibility();

            // 反弹次数超过限制，销毁投射物
            if (bounceCount >= maxBounces)
            {
                Debug.Log($"投射物反弹次数达到上限，销毁（对象: {gameObject.name}）");
                Destroy(gameObject);
            }
            return;
        }

        // 2. 玩家碰撞处理
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerCollision(collision.gameObject);
        }
    }

    // 新增：更新接取资格
    void UpdatePickupEligibility()
    {
        // 如果反弹次数达到要求，允许接取
        if (bounceCount >= requiredBouncesForPickup && !canCollideWithPlayer)
        {
            canCollideWithPlayer = true;
            Debug.Log($"投射物已反弹 {bounceCount} 次，现在可以接取了！");
        }
    }

    // 处理玩家碰撞
    void HandlePlayerCollision(GameObject player)
    {
        if (!canCollideWithPlayer)
        {
            Debug.LogWarning($"忽略玩家碰撞 - 需要至少反弹 {requiredBouncesForPickup} 次才能接取（当前: {bounceCount}）");
            return;
        }

        // 距离检查（避免极近距离误触发）
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer <= 0.3f)
        {
            Debug.LogWarning($"忽略玩家碰撞 - 距离过近（距离: {distanceToPlayer:F2}m < 0.3m）");
            return;
        }

        // 回收投射物
        Debug.Log($"玩家碰撞有效 - 回收投射物（反弹次数: {bounceCount}，距离: {distanceToPlayer:F2}m）");
        PlayerAbilities playerAbilities = player.GetComponent<PlayerAbilities>();
        if (playerAbilities != null)
        {
            playerAbilities.ReclaimProjectile(true);
        }
        Destroy(gameObject);
    }

    // 修改反弹逻辑：使用碰撞信息而不是触发器
    void BounceOffWall(Collision2D collision)
    {
        if (rb == null)
        {
            Debug.LogError("投射物缺少 Rigidbody2D 组件，无法反弹！");
            return;
        }

        // 使用碰撞接触点获取法线
        if (collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.contacts[0];
            Vector2 normal = contact.normal;
            Vector2 incomingVelocity = lastVelocity;

            // 计算反射速度
            Vector2 reflectedVelocity = Vector2.Reflect(incomingVelocity, normal);

            // 保持速度大小，但损失一些能量
            float speed = incomingVelocity.magnitude * 0.9f;
            rb.velocity = reflectedVelocity.normalized * speed;

            Debug.Log($"投射物反弹 - 入射方向: {incomingVelocity}, 法线方向: {normal}, 反弹方向: {reflectedVelocity}, 速度: {speed:F1}");
        }
    }

    // 防止粘墙的持续碰撞处理
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Ground"))
        {
            // 如果速度过小，给一个推力脱离墙面
            if (rb != null && rb.velocity.magnitude < 1f)
            {
                Vector2 pushDirection = -collision.contacts[0].normal;
                rb.AddForce(pushDirection * 3f, ForceMode2D.Impulse);
                Debug.Log("施加推力脱离墙面");
            }
        }
    }

    void OnDrawGizmos()
    {
        // 根据状态显示不同颜色
        if (canCollideWithPlayer)
        {
            Gizmos.color = Color.green; // 可接取
        }
        else if (bounceCount > 0)
        {
            Gizmos.color = Color.yellow; // 已反弹但还不够
        }
        else
        {
            Gizmos.color = Color.red; // 尚未反弹
        }

        Gizmos.DrawWireSphere(transform.position, 0.3f);

#if UNITY_EDITOR
        string statusText;
        if (canCollideWithPlayer)
        {
            statusText = $"可接取 (反弹{bounceCount}次)";
        }
        else if (bounceCount > 0)
        {
            statusText = $"还需反弹{requiredBouncesForPickup - bounceCount}次";
        }
        else
        {
            statusText = "需反弹后才能接取";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, statusText);
#endif
    }
}
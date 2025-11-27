using UnityEngine;
using System.Collections;

public class PlayerAbilities : MonoBehaviour
{
    [Header("场景配置")]
    public SceneType currentScene = SceneType.Home;
    public enum SceneType { Home, Office, Dream }

    // 能力开关
    private bool canShoot = false;
    private bool canTeleport = false;
    private bool canReverseGravity = false;

    // 发射物体相关
    [Header("发射物体设置")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    private GameObject currentProjectile;

    // 传送能力相关
    [Header("传送能力设置")]
    public float teleportMaxDistance = 5f;
    public int maxTeleportCharges = 2;
    public float teleportCooldown = 1.5f;
    public LayerMask teleportBlockLayers;

    private int currentTeleportCharges;
    private float lastTeleportTime;
    private bool isAimingTeleport = false;
    private LineRenderer aimLine;

    // 投射物传送相关
    [Header("投射物传送设置")]
    public bool canTeleportProjectile = false;
    public float projectileTeleportCooldown = 0.5f;
    private float lastProjectileTeleportTime;

    // 重力相关
    [Header("重力设置")]
    private bool isGravityReversed = false;
    private Rigidbody2D rb;
    private float originalGravity;

    // 能力冷却
    [Header("能力冷却")]
    public float shootCooldown = 0.5f;
    public float gravityCooldown = 0.8f;

    private float lastShootTime = 0f;
    private float lastGravityTime = 0f;

    // 用于处理重力反转时的跳跃冲突
    private bool isInGravityReverseMode = false;

    // 传送反馈
    [Header("传送反馈")]
    public ParticleSystem teleportParticles;
    public AudioClip teleportSound;

    // 投掷物反弹设置
    [Header("投掷物反弹设置")]
    public LayerMask bounceLayers; // 可反弹的图层
    public Color bounceReadyColor = Color.yellow; // 可反弹时的颜色
    public float bounceDetectionRange = 2f; // 反弹检测范围
    private bool isBounceReady = false; // 是否可以反弹

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            originalGravity = rb.gravityScale;
        }

        currentTeleportCharges = maxTeleportCharges;
        CreateAimLine();
        SetupAbilitiesForScene();

        Debug.Log("玩家能力系统初始化完成 - 当前场景: " + currentScene);
    }

    void Update()
    {
        HandleAbilityInput();
        CheckBounceOpportunity(); // 持续检测反弹机会
    }

    void CreateAimLine()
    {
        GameObject lineObj = new GameObject("AimLine");
        lineObj.transform.SetParent(transform);
        aimLine = lineObj.AddComponent<LineRenderer>();

        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = new Color(0, 1, 1, 0.8f);
        aimLine.endColor = new Color(0, 0.5f, 1, 0.3f);
        aimLine.startWidth = 0.15f;
        aimLine.endWidth = 0.05f;
        aimLine.positionCount = 2;
        aimLine.enabled = false;
    }

    void SetupAbilitiesForScene()
    {
        // 重置所有能力状态
        canShoot = false;
        canTeleport = false;
        canReverseGravity = false;
        canTeleportProjectile = false;
        isInGravityReverseMode = false;
        isAimingTeleport = false;

        // 重置传送相关
        currentTeleportCharges = maxTeleportCharges;
        if (aimLine != null)
            aimLine.enabled = false;

        ResetGravity();

        switch (currentScene)
        {
            case SceneType.Home:
                canShoot = true;
                Debug.Log("已启用：发射物体能力 (鼠标左键)");
                break;

            case SceneType.Office:
                canShoot = true;
                canTeleport = true;
                canTeleportProjectile = true;
                Debug.Log("已启用：发射物体能力 (鼠标左键)");
                Debug.Log("已启用：短距传送能力 (鼠标右键瞄准，释放传送)");
                Debug.Log("已启用：投射物传送能力 (T键)");
                Debug.Log("已启用：投掷物反弹能力 (R键，黄色提示时可用)");
                break;

            case SceneType.Dream:
                canReverseGravity = true;
                Debug.Log("已启用：重力逆转能力 (空格键)");
                break;
        }
    }

    void HandleAbilityInput()
    {
        if (canShoot && Input.GetMouseButtonDown(0) && Time.time >= lastShootTime + shootCooldown)
        {
            ShootProjectile();
            lastShootTime = Time.time;
        }

        // 新增：反弹输入（R键触发）
        if (Input.GetKeyDown(KeyCode.R) && isBounceReady)
        {
            ExecuteBounce();
        }

        if (canTeleport)
        {
            HandleTeleportInput();
        }

        if (canReverseGravity && Input.GetKeyDown(KeyCode.Space) && Time.time >= lastGravityTime + gravityCooldown)
        {
            isInGravityReverseMode = true;
            ReverseGravity();
            lastGravityTime = Time.time;
            StartCoroutine(ResetGravityModeFlag());
        }
    }

    void HandleTeleportInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            StartTeleportAim();
        }

        if (Input.GetMouseButtonUp(1) && isAimingTeleport)
        {
            ExecuteTeleport();
        }

        if (isAimingTeleport)
        {
            UpdateAimLine();
        }

        if (canTeleportProjectile && Input.GetKeyDown(KeyCode.T) && Time.time >= lastProjectileTeleportTime + projectileTeleportCooldown && currentProjectile != null)
        {
            TeleportProjectile();
            lastProjectileTeleportTime = Time.time;
        }
    }

    void StartTeleportAim()
    {
        if (currentTeleportCharges <= 0 || Time.time < lastTeleportTime + teleportCooldown)
            return;

        isAimingTeleport = true;
        aimLine.enabled = true;
        Debug.Log("开始传送瞄准");
    }

    void UpdateAimLine()
    {
        if (!isAimingTeleport || aimLine == null) return;

        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 playerPos = transform.position;
        Vector3 direction = (mousePos - playerPos).normalized;

        Vector3 targetPos = mousePos;
        float distance = Vector3.Distance(playerPos, mousePos);

        // 限制最大距离
        if (distance > teleportMaxDistance)
        {
            targetPos = playerPos + direction * teleportMaxDistance;
        }

        // 检查目标位置是否在墙内
        Collider2D wallCheck = Physics2D.OverlapCircle(targetPos, 0.3f, teleportBlockLayers);
        bool willHitWall = (wallCheck != null);

        // 更新瞄准线端点
        aimLine.SetPosition(0, playerPos);
        aimLine.SetPosition(1, targetPos);

        // 根据是否会撞墙改变颜色
        if (willHitWall)
        {
            aimLine.startColor = Color.red;
            aimLine.endColor = Color.red;
            aimLine.positionCount = 3;
            aimLine.SetPosition(2, targetPos);
        }
        else
        {
            aimLine.startColor = new Color(0, 1, 1, 0.8f);
            aimLine.endColor = new Color(0, 0.5f, 1, 0.3f);
            aimLine.positionCount = 2;
        }

        Debug.Log($"瞄准线更新 - 目标: {targetPos}, 撞墙: {willHitWall}");
    }

    void ExecuteTeleport()
    {
        if (!isAimingTeleport)
        {
            Debug.Log("传送失败：不在瞄准状态");
            return;
        }

        if (currentTeleportCharges <= 0)
        {
            Debug.Log($"传送失败：次数不足，当前次数: {currentTeleportCharges}");
            return;
        }

        if (Time.time < lastTeleportTime + teleportCooldown)
        {
            Debug.Log($"传送失败：冷却中，剩余时间: {lastTeleportTime + teleportCooldown - Time.time:F1}秒");
            return;
        }

        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 playerPos = transform.position;
        Vector3 direction = (mousePos - playerPos).normalized;

        Vector3 targetPos = mousePos;
        float distance = Vector3.Distance(playerPos, mousePos);

        // 限制最大距离
        if (distance > teleportMaxDistance)
        {
            targetPos = playerPos + direction * teleportMaxDistance;
        }

        // 轻量级墙壁检测：如果目标点在墙内，尝试找到墙外最近的点
        Collider2D wallCheck = Physics2D.OverlapCircle(targetPos, 0.3f, teleportBlockLayers);
        if (wallCheck != null)
        {
            Debug.Log($"目标点在墙内，尝试调整位置");
            Vector3 adjustedPos = FindNearestValidPosition(playerPos, targetPos);
            if (adjustedPos != playerPos)
            {
                targetPos = adjustedPos;
            }
            else
            {
                Debug.LogWarning("找不到有效位置，取消传送");
                isAimingTeleport = false;
                aimLine.enabled = false;
                return;
            }
        }

        Debug.Log($"最终传送位置: {targetPos}");

        // 传送反馈（传送前）
        if (teleportParticles != null)
        {
            Instantiate(teleportParticles, transform.position, Quaternion.identity);
        }
        if (teleportSound != null)
        {
            AudioSource.PlayClipAtPoint(teleportSound, transform.position);
        }

        // 保存并恢复速度
        Vector2 previousVelocity = Vector2.zero;
        if (rb != null)
        {
            previousVelocity = rb.velocity;
        }

        // 执行传送
        transform.position = targetPos;

        // 传送反馈（传送后）
        if (teleportParticles != null)
        {
            Instantiate(teleportParticles, transform.position, Quaternion.identity);
        }

        // 恢复速度
        if (rb != null)
        {
            rb.velocity = previousVelocity;
        }

        // 更新状态
        currentTeleportCharges--;
        lastTeleportTime = Time.time;

        // 重置瞄准状态
        isAimingTeleport = false;
        aimLine.enabled = false;

        Debug.Log($"传送完成！剩余次数: {currentTeleportCharges}");
    }

    Vector3 FindNearestValidPosition(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        float maxDistance = Vector3.Distance(from, to);

        // 从目标点向回找空位，步长0.1f
        for (float dist = maxDistance; dist > 0; dist -= 0.1f)
        {
            Vector3 testPos = from + direction * dist;
            Collider2D hit = Physics2D.OverlapCircle(testPos, 0.3f, teleportBlockLayers);
            if (hit == null)
            {
                return testPos;
            }
        }

        // 如果找不到空位，返回原始位置（不传送）
        return from;
    }

    void TeleportProjectile()
    {
        if (currentProjectile == null) return;

        Vector3 mousePos = GetMouseWorldPosition();
        currentProjectile.transform.position = mousePos;

        Rigidbody2D projRb = currentProjectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.velocity = Vector2.zero;
        }

        Debug.Log("投射物已传送");
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private IEnumerator ResetGravityModeFlag()
    {
        yield return new WaitForEndOfFrame();
        isInGravityReverseMode = false;
    }

    void ShootProjectile()
    {
        if (currentProjectile != null) return;

        Debug.Log("发射物体！");

        if (Camera.main == null)
        {
            Debug.LogError("主相机未找到！");
            return;
        }

        Vector3 mousePos = GetMouseWorldPosition();
        Vector2 direction = (mousePos - transform.position).normalized;

        // 修改：根据鼠标方向生成在人物前方一定距离，而不是固定右侧
        float spawnDistance = 0.8f; // 生成距离，避免立即碰撞
        Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnDistance);

        currentProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        Rigidbody2D projRb = currentProjectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.velocity = direction * projectileSpeed;

            // 添加：临时忽略与玩家的碰撞
            StartCoroutine(IgnorePlayerCollisionTemporarily(projRb));
        }
        else
        {
            Debug.LogError("投射物缺少Rigidbody2D组件！");
        }

        Destroy(currentProjectile, 3f);
    }

    // 新增：临时忽略与玩家的碰撞
    private IEnumerator IgnorePlayerCollisionTemporarily(Rigidbody2D projRb)
    {
        if (projRb == null) yield break;

        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D projectileCollider = projRb.GetComponent<Collider2D>();

        if (playerCollider != null && projectileCollider != null)
        {
            // 忽略碰撞
            Physics2D.IgnoreCollision(playerCollider, projectileCollider, true);

            // 0.5秒后恢复碰撞
            yield return new WaitForSeconds(0.5f);

            Physics2D.IgnoreCollision(playerCollider, projectileCollider, false);
            Debug.Log("恢复投掷物与玩家的碰撞");
        }
    }

    void ReverseGravity()
    {
        isGravityReversed = !isGravityReversed;
        if (isGravityReversed)
        {
            rb.gravityScale = -originalGravity;
            transform.localScale = new Vector3(transform.localScale.x, -Mathf.Abs(transform.localScale.y), transform.localScale.z);
            Debug.Log("重力逆转！");
        }
        else
        {
            rb.gravityScale = originalGravity;
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Abs(transform.localScale.y), transform.localScale.z);
            Debug.Log("重力恢复正常");
        }
    }

    void ResetGravity()
    {
        isGravityReversed = false;
        if (rb != null)
        {
            rb.gravityScale = originalGravity;
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Abs(transform.localScale.y), transform.localScale.z);
        }
    }

    public void ReclaimProjectile(bool refreshDashAndHover = false)
    {
        if (currentProjectile != null)
        {
            Destroy(currentProjectile);
            currentProjectile = null;
            Debug.Log("投射物已被回收");
        }

        if (refreshDashAndHover)
        {
            RefreshDash();
            RefreshTeleport();
            StartCoroutine(HoverEffect());
        }
    }

    private void RefreshDash()
    {
        gameObject.SendMessage("ResetDash", SendMessageOptions.DontRequireReceiver);
        Debug.Log("冲刺能力已刷新！");
    }

    private void RefreshTeleport()
    {
        if (currentTeleportCharges < maxTeleportCharges)
        {
            currentTeleportCharges++;
            Debug.Log($"传送次数刷新！当前次数: {currentTeleportCharges}/{maxTeleportCharges}");
        }
        else
        {
            Debug.Log($"传送次数已满: {currentTeleportCharges}/{maxTeleportCharges}");
        }
    }

    private IEnumerator HoverEffect()
    {
        Debug.Log("获得滞空效果！");
        if (rb != null)
        {
            float originalGravity = rb.gravityScale;
            Vector2 originalVelocity = rb.velocity;
            rb.gravityScale = originalGravity * 0.2f;
            rb.velocity = new Vector2(originalVelocity.x, 5f);
            yield return new WaitForSeconds(0.3f);

            float timer = 0f;
            float transitionTime = 0.2f;
            while (timer < transitionTime)
            {
                timer += Time.deltaTime;
                rb.gravityScale = Mathf.Lerp(originalGravity * 0.2f, originalGravity, timer / transitionTime);
                yield return null;
            }
            rb.gravityScale = originalGravity;
            Debug.Log("滞空效果结束");
        }
    }

    public bool IsInGravityReverseMode()
    {
        return isInGravityReverseMode;
    }

    public void SetCurrentScene(SceneType scene)
    {
        currentScene = scene;
        SetupAbilitiesForScene();
    }

    void OnDrawGizmos()
    {
        if (isAimingTeleport)
        {
            Gizmos.color = Color.magenta;
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 playerPos = transform.position;
            float distance = Vector3.Distance(playerPos, mousePos);
            Vector3 direction = (mousePos - playerPos).normalized;
            Vector3 targetPos = (distance > teleportMaxDistance) ? (playerPos + direction * teleportMaxDistance) : mousePos;

            Gizmos.DrawLine(playerPos, targetPos);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }

        // 新增：显示反弹检测范围
        if (currentProjectile != null)
        {
            Gizmos.color = isBounceReady ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(currentProjectile.transform.position, bounceDetectionRange);
        }
    }

    // 新增方法：检测反弹机会
    void CheckBounceOpportunity()
    {
        if (currentProjectile == null)
        {
            isBounceReady = false;
            return;
        }

        // 检测投掷物周围是否有可反弹的表面
        Collider2D[] bounceTargets = Physics2D.OverlapCircleAll(
            currentProjectile.transform.position,
            bounceDetectionRange,
            bounceLayers
        );

        isBounceReady = bounceTargets.Length > 0;

        // 更新投掷物颜色提示
        UpdateProjectileColor();
    }

    // 新增方法：更新投掷物颜色
    // 在 UpdateProjectileColor 方法中修改颜色逻辑
    void UpdateProjectileColor()
    {
        if (currentProjectile == null) return;

        ProjectileBehavior projBehavior = currentProjectile.GetComponent<ProjectileBehavior>();
        SpriteRenderer projRenderer = currentProjectile.GetComponent<SpriteRenderer>();

        if (projRenderer != null && projBehavior != null)
        {
            // 根据反弹状态显示不同颜色
            if (projBehavior.canCollideWithPlayer)
            {
                projRenderer.color = Color.green; // 可接取
            }
            else if (projBehavior.bounceCount > 0)
            {
                projRenderer.color = Color.yellow; // 已反弹但还不够
            }
            else
            {
                projRenderer.color = Color.white; // 尚未反弹
            }
        }
    }

    // 新增方法：执行反弹
    void ExecuteBounce()
    {
        if (!isBounceReady || currentProjectile == null) return;

        Rigidbody2D projRb = currentProjectile.GetComponent<Rigidbody2D>();
        if (projRb == null) return;

        // 找到最近的反弹表面
        Collider2D[] bounceTargets = Physics2D.OverlapCircleAll(
            currentProjectile.transform.position,
            bounceDetectionRange,
            bounceLayers
        );

        if (bounceTargets.Length > 0)
        {
            Vector2 closestSurface = FindClosestBounceSurface(bounceTargets);
            Vector2 incomingDirection = projRb.velocity.normalized;
            Vector2 surfaceNormal = (closestSurface - (Vector2)currentProjectile.transform.position).normalized;

            // 计算反射方向
            Vector2 bounceDirection = Vector2.Reflect(incomingDirection, surfaceNormal);

            // 应用新的速度
            projRb.velocity = bounceDirection * projectileSpeed;

            Debug.Log("投掷物反弹！新方向: " + bounceDirection);

            // 反弹后的冷却
            StartCoroutine(BounceCooldown());
        }
    }

    // 新增方法：找到最近的反弹表面
    Vector2 FindClosestBounceSurface(Collider2D[] surfaces)
    {
        Vector2 projectilePos = currentProjectile.transform.position;
        float closestDistance = float.MaxValue;
        Vector2 closestPoint = projectilePos;

        foreach (Collider2D surface in surfaces)
        {
            Vector2 surfacePoint = surface.ClosestPoint(projectilePos);
            float distance = Vector2.Distance(projectilePos, surfacePoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = surfacePoint;
            }
        }

        return closestPoint;
    }

    // 新增协程：反弹冷却
    private IEnumerator BounceCooldown()
    {
        isBounceReady = false;
        yield return new WaitForSeconds(0.5f); // 短暂冷却防止连续反弹
    }
}
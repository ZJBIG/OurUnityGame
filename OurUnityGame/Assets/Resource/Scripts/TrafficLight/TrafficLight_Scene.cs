using System.Collections;
using UnityEngine;

public class TrafficLight_Scene : MonoBehaviour
{
    Transform Player;
    SpriteRenderer sp;
    private bool isTrigger = false;
    public Sprite Green, Red;
    public Vector2 TeleportTo;
    [Header("范围内检测玩家")]
    public float DetectDistance;
    [Header("对应要播放动画的变异交通灯(场景中的物体)")]
    public Transform TrafficLight_Enemy;
    [Header("触手预制体")]
    public GameObject HandPrefab;
    public bool isRed => sp.sprite == Red;

    [Header("红绿灯切换时间")]
    public float SwitchTime;
    private void Start()
    {
        sp = GetComponent<SpriteRenderer>();
        Player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(SwitchColor());
        TrafficLight_Enemy.GetComponent<Animator>().enabled = false;
    }
    private void Update()
    {
        float distance = Vector2.Distance(Player.position, transform.position);
        if (distance <= DetectDistance)
            if (isRed)
                if (Player.GetComponent<Rigidbody2D>().velocity.magnitude >= 0.01f)
                    if (!isTrigger)
                        StartCoroutine(TriggerEnemy());
    }
    private IEnumerator TriggerEnemy()
    {
        isTrigger = true;
        sp.sprite = Red;


        Player.GetComponent<PlayerCtrl>().LockMoveTime = 999f;
        Vector2 originalPlayerPosition = Player.position;
        var enemyAnimator = TrafficLight_Enemy.GetComponent<Animator>();
        enemyAnimator.enabled = true;
        yield return null;

        GameObject hand = Instantiate(HandPrefab, Player.position + new Vector3(-0.5f, 0.2f), Quaternion.identity);

        AnimatorStateInfo stateInfo = enemyAnimator.GetCurrentAnimatorStateInfo(0);
        float fadeDuration = enemyAnimator.GetCurrentAnimatorStateInfo(0).length;
        float timer = 0f;

        SpriteRenderer playerRenderer = Player.GetComponent<SpriteRenderer>();
        Color originalColor = playerRenderer.color;
        Rigidbody2D PlayerRB = Player.GetComponent<Rigidbody2D>();
        Player.position = originalPlayerPosition;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1.0f - Mathf.Clamp01(timer / fadeDuration);
            playerRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            hand.transform.position = Player.position + new Vector3(-0.5f, 0.2f);

            yield return null;
        }
        yield return new WaitForSecondsRealtime(1f);

        Destroy(hand);

        playerRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1.0f);
        Player.position = TeleportTo;
        Player.GetComponent<PlayerCtrl>().LockMoveTime = 0f;
        isTrigger = false;

        TrafficLight_Enemy.GetComponent<Animator>().Rebind();
        TrafficLight_Enemy.GetComponent<Animator>().enabled = false;

    }
    IEnumerator SwitchColor()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(SwitchTime - 0.8f);

            Vector2 originaPosition = transform.position;
            if (!isTrigger)
                for (int i = 0; i < 10; i++)
                {
                    transform.position = originaPosition + Random.insideUnitCircle * 0.01f;
                    yield return new WaitForSecondsRealtime(0.08f);
                }

            transform.position = originaPosition;

            if (!isTrigger)
                sp.sprite = isRed ? Green : Red;
            else
                sp.sprite = Red;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = !sp ? Color.red : isRed ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, DetectDistance);
    }
}

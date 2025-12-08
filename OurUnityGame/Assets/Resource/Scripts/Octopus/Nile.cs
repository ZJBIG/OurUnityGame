using System.Collections;
using UnityEngine;

public class Nile : MonoBehaviour
{
    Animator ani;
    [Header("使章鱼受伤所需要的竖直速度")]
    public float HurtSpeed;
    [Header("每次的受伤时间")]
    public float HurtTime;
    private bool Hurt
    {
        get => transform.parent.GetChild(0).GetComponent<Octopus>().isHurt;
        set => transform.parent.GetChild(0).GetComponent<Octopus>().isHurt = value;
    }
    void Start()
    {
        ani = transform.parent.GetChild(0).GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !Hurt)
        {
            var rb = collision.GetComponent<Rigidbody2D>();
            if (rb.velocity.y <= -HurtSpeed)
            {
                StopCoroutine(BeingHurt());
                StartCoroutine(BeingHurt());
            }
        }
    }
    IEnumerator BeingHurt()
    {
        Hurt = true;
        ani.SetInteger("Status", 1);
        yield return new WaitForSecondsRealtime(HurtTime);
        ani.SetInteger("Status", 0);
        Hurt = false;
    }
}

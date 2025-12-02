using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Nile : MonoBehaviour
{
    Animator ani;
    public float HurtSpeed;//使章鱼受伤所需要的竖直速度;
    public float HurtTime;
    [HideInInspector] public bool isHurt;
    void Start()
    {
        ani = transform.parent.GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
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
        isHurt = true;
        ani.SetInteger("Status", 1);
        yield return new WaitForSeconds(HurtTime);
        ani.SetInteger("Status", 0);
        isHurt = false;
    }
}

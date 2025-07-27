using System.Collections.Generic;
using UnityEngine;

public class FlowerJump : MonoBehaviour
{
    public float jumpForce = 10f;
    public float cooldown = 2f;

    private bool isOnCooldown = false;

    void Awake()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isOnCooldown) return;
        if (!collision.CompareTag("Player")) return;
        Debug.Log("Contact Detected");
        // 플레이어의 Rigidbody2D 얻어서 힘 가하기
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            Debug.Log("Jump yo");
        }
        
        // 쿨타임 시작
        StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }
    
}

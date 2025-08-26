using System.Collections.Generic;
using UnityEngine;

public class FlowerJump : MonoBehaviour
{
    private float jumpForce = 20f;
    private float cooldown = 2f;
    private bool isOnCooldown = false;

    private Vector2 checkSize = new Vector2(0.8f, 0.8f); // 점프대 크기에 맞춰 조정
    private Vector2 checkOffset = new Vector2(0, 0f); // 점프대 중심에서 위로 살짝 올려서

    void Update()
    {
        if (isOnCooldown) return;

        // 점프대 위에 플레이어가 있는지 OverlapBox로 체크
        Vector2 center = (Vector2)transform.position + checkOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, checkSize, 0f, LayerMask.GetMask("Player"));

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    StartCoroutine(CooldownRoutine());
                    break;
                }
            }
        }
    }
    private System.Collections.IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }

    // Scene에서 OverlapBox 시각화 (선택)
    void OnDrawGizmosSelected()
    {
        Vector2 center = (Vector2)transform.position + checkOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, checkSize);
    }
    
}

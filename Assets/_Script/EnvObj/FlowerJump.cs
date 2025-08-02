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

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isOnCooldown) return;
        if (!collision.CompareTag("Player")) return;
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        
        StartCoroutine(CooldownRoutine());
    }
    
    

    private System.Collections.IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }
    
}

using UnityEngine;

public class Bomb : MonoBehaviour
{
    // Call this after instantiating the bomb to throw it in a parabolic arc
    public void Throw(Vector2 direction, float force)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Ensure physics is enabled
            rb.linearVelocity = Vector2.zero; // Reset velocity
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        }
    }
    public float autoExplosionDelay = 5f;  // If it never hits, explode after this
    public float stickExplosionDelay = 2f; // Time after sticking to player
    public GameObject explosionEffect;
    public int damage = 20;

    private bool stuckToPlayer = false;
    private Transform playerTransform;

    private void Start()
    {
    // Auto-explode if nothing happens
    Invoke(nameof(Explode), autoExplosionDelay);

    // Example usage (remove/comment out in production):
    // Throw(new Vector2(1, 1), 10f); // Throws up and to the right
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!stuckToPlayer && collision.gameObject.CompareTag("Player"))
        {
            StickToPlayer(collision.gameObject.transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // In case player walks over idle bomb
        if (!stuckToPlayer && collision.CompareTag("Player"))
        {
            StickToPlayer(collision.transform);
        }
    }

    void StickToPlayer(Transform player)
    {
        stuckToPlayer = true;
        playerTransform = player;

        // Remove physics so it follows perfectly
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable collider so it doesn't mess with movement
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Parent bomb to player so it moves with them
        transform.SetParent(playerTransform);
        transform.localPosition = Vector3.up * 0.5f; // Offset on body

        // Start short fuse
        CancelInvoke(nameof(Explode));
        Invoke(nameof(Explode), stickExplosionDelay);

        Debug.Log("Bomb stuck to player! Exploding soon...");
    }

    void Explode()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        if (stuckToPlayer && playerTransform != null)
        {
            Debug.Log("Player takes " + damage + " damage!");
            // Apply damage to player here
        }

        Destroy(gameObject);
    }
}

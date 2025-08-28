using UnityEngine;

public class Bomb : MonoBehaviour
{
    // Call this after instantiating the bomb to throw it in a parabolic arc
    // Set the velocity directly for a true parabolic arc
    public void SetVelocity(Vector2 velocity)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = velocity;
        }
    }
    public float autoExplosionDelay = 5f;  // If it never hits, explode after this
    public float stickExplosionDelay = 2f; // Time after sticking to player
    public GameObject explosionEffect;
    public int damage = 20;

    private bool stuckToPlayer = false;
    private Transform playerTransform;

    private Collider2D bombCollider;
    private Vector3 targetPosition;
    private bool colliderEnabled = false;

    private void Start()
    {
        // Auto-explode if nothing happens
        Invoke(nameof(Explode), autoExplosionDelay);

        bombCollider = GetComponent<Collider2D>();
        if (bombCollider != null)
            bombCollider.enabled = false; // Disable collider at spawn
    }

    // Call this from BunnyBoss.cs after instantiating the bomb
    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
    }

    private void Update()
    {
        // Only enable collider when close to target position and not stuck to player
        if (!colliderEnabled && !stuckToPlayer && targetPosition != Vector3.zero)
        {
            float distance = Vector2.Distance(transform.position, targetPosition);
            if (distance < 0.5f) // Adjust threshold as needed
            {
                if (bombCollider != null)
                    bombCollider.enabled = true;
                colliderEnabled = true;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!stuckToPlayer && collision.gameObject.CompareTag("Player"))
        {
            StickToPlayer(collision.gameObject.transform);
        }
        // If bomb lands on ground or platform, stop sliding
        if (!stuckToPlayer && !collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
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
    if (bombCollider != null) bombCollider.enabled = false;

        // Parent bomb to player so it moves with them
        transform.SetParent(playerTransform);
        transform.localPosition = Vector3.up * 0.5f; // Offset on body

        // Start short fuse
        CancelInvoke(nameof(Explode));
        Invoke(nameof(Explode), stickExplosionDelay);
    }

    void Explode()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        if (stuckToPlayer && playerTransform != null)
        {
            GameManager.Instance.PStats.TakeDamage(damage);

        }

        Destroy(gameObject);
    }
}

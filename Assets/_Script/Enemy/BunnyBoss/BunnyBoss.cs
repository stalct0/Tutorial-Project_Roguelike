using UnityEngine;
using System.Collections;

public class BossBunny : MonoBehaviour
{
    public Transform[] platforms; // Assign all platform positions in inspector
    public Transform throwPoint;
    public GameObject bombPrefab;

    [Header("Jump Settings")]
    public float gravity = 9.81f;
    public float jumpHeight = 4f;
    public float jumpCooldown = 2f;

    [Header("Bomb Settings")]
    public float bombPrepTime = 1f;
    public int minBombs = 1;
    public int maxBombs = 3;
    public float throwHeight = 2f;

    [Header("Fall Damage")]
    public float fallDamagePerUnit = 5f;
    public float groundY = 0f; // Y position of ground

    private bool isJumping = false;
    private bool onPlatform = false;
    private Rigidbody2D rb;
    private Transform currentPlatform;
    private Transform player;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentPlatform = platforms[Random.Range(0, platforms.Length)];
        transform.position = currentPlatform.position;
        StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        while (true)
        {
            // Pick new platform
            Transform targetPlatform = GetRandomPlatform(currentPlatform);
            yield return JumpToPlatform(targetPlatform);

            // Bomb attack phase
            int bombsToThrow = Random.Range(minBombs, maxBombs + 1);
            for (int i = 0; i < bombsToThrow; i++)
            {
                yield return new WaitForSeconds(bombPrepTime);
                ThrowBomb();
            }

            // Wait before next jump
            yield return new WaitForSeconds(jumpCooldown);
        }
    }

    Transform GetRandomPlatform(Transform exclude)
    {
        Transform chosen;
        do
        {
            chosen = platforms[Random.Range(0, platforms.Length)];
        }
        while (chosen == exclude);
        return chosen;
    }

    IEnumerator JumpToPlatform(Transform target)
    {
        isJumping = true;
        onPlatform = false;
        Vector2 velocity = CalculateParabolaVelocity(transform.position, target.position, jumpHeight);
        rb.linearVelocity = velocity;

        // Wait until close to target Y
        while (!onPlatform)
        {
            if (Mathf.Abs(transform.position.y - target.position.y) < 0.2f &&
                Mathf.Abs(transform.position.x - target.position.x) < 0.2f)
            {
                transform.position = target.position;
                rb.linearVelocity = Vector2.zero;
                onPlatform = true;
                currentPlatform = target;
                isJumping = false;
            }

            // If bunny fell off screen
            if (transform.position.y <= groundY + 0.1f)
            {
                TakeFallDamage(currentPlatform.position.y - groundY);
                rb.linearVelocity = Vector2.zero;
                onPlatform = true; // Now on ground
                isJumping = false;
            }

            yield return null;
        }
    }

    void ThrowBomb()
    {
        Vector2 velocity = CalculateParabolaVelocity(throwPoint.position, player.position, throwHeight);
        GameObject bombObj = Instantiate(bombPrefab, throwPoint.position, Quaternion.identity);
        Bomb bomb = bombObj.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.Throw(velocity, 1f); // The force parameter is not used if you pass velocity directly, but keep for API compatibility
        }
        else
        {
            // fallback: set velocity directly if Bomb script missing
            Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = velocity;
        }
    }

    Vector2 CalculateParabolaVelocity(Vector2 start, Vector2 target, float height)
    {
        float displacementY = target.y - start.y;
        Vector2 displacementX = new Vector2(target.x - start.x, 0);

        // Ensure height is always above the target, and gravity is positive
        float safeHeight = Mathf.Max(height, displacementY + 0.5f);
        float safeGravity = Mathf.Max(gravity, 0.01f);

        float timeUp = Mathf.Sqrt(Mathf.Max(0.01f, 2 * safeHeight / safeGravity));
        float timeDown = Mathf.Sqrt(Mathf.Max(0.01f, 2 * Mathf.Max(0.01f, safeHeight - displacementY) / safeGravity));
        float totalTime = timeUp + timeDown;

        float vx = (Mathf.Abs(totalTime) > 0.01f) ? displacementX.x / totalTime : 0f;
        float vy = Mathf.Sqrt(2 * safeGravity * safeHeight);

        // If any value is NaN or Infinity, fallback to a simple horizontal jump
        if (float.IsNaN(vx) || float.IsInfinity(vx) || float.IsNaN(vy) || float.IsInfinity(vy))
        {
            vx = displacementX.x > 0 ? 5f : -5f;
            vy = 5f;
        }

        return new Vector2(vx, vy);
    }

    public void OnHitByPlayer(Vector2 knockback)
    {
        if (onPlatform && !isJumping)
        {
            rb.linearVelocity = knockback;
            StartCoroutine(CheckForFall());
        }
    }

    IEnumerator CheckForFall()
    {
        yield return new WaitForSeconds(0.2f);
        if (transform.position.y < currentPlatform.position.y - 0.5f)
        {
            // Fell
            float fallHeight = currentPlatform.position.y - transform.position.y;
            TakeFallDamage(fallHeight);
        }
    }

    void TakeFallDamage(float height)
    {
        float damage = Mathf.Max(0, height * fallDamagePerUnit);
        Debug.Log("Bunny takes " + damage + " fall damage!");
        // Apply damage here
    }
}

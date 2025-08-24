using UnityEngine;
using System.Collections;

public class BossBunny : MonoBehaviour
{
    public Transform[] platforms;
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
    public float groundY = 0f;

    private bool isJumping = false;
    private bool onPlatform = false;
    private bool isInvulnerable = false;
    private Rigidbody2D rb;
    private Transform currentPlatform;
    private Transform player;
    private Collider2D bossCollider;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError("BossBunny: Player not found! Tag the player GameObject as 'Player'.");

        currentPlatform = platforms[Random.Range(0, platforms.Length)];
        transform.position = currentPlatform.position;
        StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        while (true)
        {
            Transform targetPlatform = GetRandomPlatform(currentPlatform);
            yield return MoveToPlatform(targetPlatform);

            int bombsToThrow = Random.Range(minBombs, maxBombs + 1);
            for (int i = 0; i < bombsToThrow; i++)
            {
                yield return new WaitForSeconds(bombPrepTime);
                ThrowBomb();
            }

            yield return new WaitForSeconds(jumpCooldown);
        }
    }

    Transform GetRandomPlatform(Transform exclude)
    {
        Transform chosen;
        do
        {
            chosen = platforms[Random.Range(0, platforms.Length)];
        } while (chosen == exclude);

        Debug.Log($"BossBunny: Moving to platform {chosen.name} at {chosen.position}");
        return chosen;
    }

    IEnumerator MoveToPlatform(Transform target)
    {
        isJumping = true;
        isInvulnerable = true;
        onPlatform = false;

        if (target.position.y > transform.position.y)
        {
            // Jump upwards
            Vector2 velocity = CalculateParabolaVelocityCustomGravity(
                transform.position,
                target.position,
                jumpHeight * 1.5f,
                gravity * 0.5f
            );

            bossCollider.enabled = false; // disable collisions while airborne
            rb.linearVelocity = velocity;

            while (!onPlatform)
            {
                if (Vector2.Distance(transform.position, target.position) < 0.2f)
                {
                    LandOnPlatform(target);
                }
                yield return null;
            }
        }
        else
        {
            // Drop down
            bossCollider.enabled = false; // phase through platform
            rb.linearVelocity = new Vector2(0, -10f); // strong downward velocity

            while (!onPlatform)
            {
                if (transform.position.y <= target.position.y + 0.1f)
                {
                    LandOnPlatform(target);
                }
                yield return null;
            }
        }
    }

    void LandOnPlatform(Transform target)
    {
        transform.position = target.position;
        rb.linearVelocity = Vector2.zero;
        onPlatform = true;
        currentPlatform = target;
        isJumping = false;
        isInvulnerable = false;
        bossCollider.enabled = true; // re-enable collisions
        Debug.Log($"BossBunny Landed on {target.name}");
    }

    Vector2 CalculateParabolaVelocityCustomGravity(Vector2 start, Vector2 target, float height, float customGravity)
    {
        float displacementY = target.y - start.y;
        Vector2 displacementX = new Vector2(target.x - start.x, 0);

        float safeHeight = Mathf.Max(height, displacementY + 0.5f);
        float safeGravity = Mathf.Max(customGravity, 0.01f);

        float timeUp = Mathf.Sqrt(Mathf.Max(0.01f, 2 * safeHeight / safeGravity));
        float timeDown = Mathf.Sqrt(Mathf.Max(0.01f, 2 * Mathf.Max(0.01f, safeHeight - displacementY) / safeGravity));
        float totalTime = timeUp + timeDown;

        float vx = displacementX.x / totalTime;
        float vy = Mathf.Sqrt(2 * safeGravity * safeHeight);

        return new Vector2(vx, vy);
    }

    void ThrowBomb()
    {
        Vector2 velocity = CalculateParabolaVelocity(throwPoint.position, player.position, throwHeight);
        GameObject bombObj = Instantiate(bombPrefab, throwPoint.position, Quaternion.identity);

        Bomb bomb = bombObj.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.SetVelocity(velocity);
            bomb.SetTarget(player.position);
        }
        else
        {
            Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = velocity;
        }
    }

    Vector2 CalculateParabolaVelocity(Vector2 start, Vector2 target, float height)
    {
        float displacementY = target.y - start.y;
        Vector2 displacementX = new Vector2(target.x - start.x, 0);

        float safeHeight = Mathf.Max(height, displacementY + 0.5f);
        float safeGravity = Mathf.Max(gravity, 0.01f);

        float timeUp = Mathf.Sqrt(2 * safeHeight / safeGravity);
        float timeDown = Mathf.Sqrt(2 * Mathf.Max(0.01f, safeHeight - displacementY) / safeGravity);
        float totalTime = timeUp + timeDown;

        float vx = displacementX.x / totalTime;
        float vy = Mathf.Sqrt(2 * safeGravity * safeHeight);

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
            float fallHeight = currentPlatform.position.y - transform.position.y;
            TakeFallDamage(fallHeight);
        }
    }

    void TakeFallDamage(float height)
    {
        float damage = Mathf.Max(0, height * fallDamagePerUnit);
        Debug.Log("Bunny takes " + damage + " fall damage!");
        // Apply damage logic here
    }
}

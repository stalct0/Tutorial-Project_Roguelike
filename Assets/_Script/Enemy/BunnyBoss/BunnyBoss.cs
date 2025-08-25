using UnityEngine;
using System.Collections;

public class BossBunny : MonoBehaviour
{
    public Transform[] platforms;
    public Transform throwPoint;
    public GameObject bombPrefab;

    [Header("Jump Settings")]
    public float jumpAirTime = 1.4f; // time to complete jump arc
    public float jumpHeight = 4f;    // peak height of jump
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
        rb.isKinematic = true; // disable physics-driven motion

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
        bossCollider.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = target.position;

        float timer = 0f;

        while (timer < jumpAirTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / jumpAirTime);

            // smooth horizontal lerp
            float x = Mathf.Lerp(startPos.x, endPos.x, t);

            // vertical arc with sine
            float y = Mathf.Lerp(startPos.y, endPos.y, t) + Mathf.Sin(t * Mathf.PI) * jumpHeight;

            transform.position = new Vector3(x, y, startPos.z);

            yield return null;
        }

        LandOnPlatform(target);
    }

    void LandOnPlatform(Transform target)
    {
        transform.position = target.position;
        onPlatform = true;
        currentPlatform = target;
        isJumping = false;
        isInvulnerable = false;
        bossCollider.enabled = true;
        Debug.Log($"BossBunny Landed on {target.name}");
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
        float g = 9.81f;

        float timeUp = Mathf.Sqrt(2 * safeHeight / g);
        float timeDown = Mathf.Sqrt(2 * Mathf.Max(0.01f, safeHeight - displacementY) / g);
        float totalTime = timeUp + timeDown;

        float vx = displacementX.x / totalTime;
        float vy = Mathf.Sqrt(2 * g * safeHeight);

        return new Vector2(vx, vy);
    }

    public void OnHitByPlayer(Vector2 knockback)
    {
        // Boss can't be knocked with physics anymore
        Debug.Log("BossBunny was hit! (no knockback with kinematic jump).");
    }
}

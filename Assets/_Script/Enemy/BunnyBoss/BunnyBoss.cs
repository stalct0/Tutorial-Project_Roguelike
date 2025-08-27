using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;
using System.Linq; // ← 상단 using 목록에 추가

public class BossBunny : MonoBehaviour
{
    public Transform[] platforms;
    public Transform throwPoint;
    public GameObject bombPrefab;

    [Header("Auto-Discover Platforms")]
    [SerializeField] string platformContainerName = "BossPlatforms";
    [SerializeField] string platformTag = "BossPlatform";
    [SerializeField] string platformNameContainsA = "BossPlatform";
    [SerializeField] string platformNameContainsB = "PlatformPoint";
    [SerializeField] float platformScanTimeout = 5f; // 최대 대기(초)
    
    [Header("Jump Settings")]
    public float jumpAirTime = 1.4f; // time to complete jump arc
    public float jumpHeight = 4f;    // peak height of jump
    public float jumpCooldown = 2f;

    [Header("Bomb Settings")]
    [NonSerialized] public float bombPrepTime = 0.5f;
    [NonSerialized] public int minBombs = 2;
    [NonSerialized] public int maxBombs = 6;
    [NonSerialized] public float throwHeight = 2f;

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

        // Set Dynamic initially so physics works when idle
        rb.bodyType = RigidbodyType2D.Dynamic;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError("BossBunny: Player not found! Tag the player GameObject as 'Player'.");
        
        onPlatform = true;
        
        StartCoroutine(SetupAndRun());
    }
    IEnumerator SetupAndRun()
    {
        float t = 0f;


            // 맵 생성(타일/오브젝트 복사)이 끝날 때까지 최대 timeout 동안 대기/재시도
            while (!TryAutoFindPlatforms())
            {
                if (t > platformScanTimeout) break;
                t += Time.deltaTime;
                yield return null; // 다음 프레임
            }

        // 시작 플랫폼 위치로 워프
        currentPlatform = platforms[Random.Range(0, platforms.Length)];
        transform.position = currentPlatform.position;
        onPlatform = true;

        // 메인 루프 시작
        StartCoroutine(BossLoop());
    }
    IEnumerator BossLoop()
    {
        while (true)
        {
            // ✅ Only jump if on a platform AND not falling
            yield return new WaitUntil(() => onPlatform && !isJumping && Mathf.Abs(rb.linearVelocity.y) < 0.01f);

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

        // ✅ Set Kinematic while jump is scripted
        rb.bodyType = RigidbodyType2D.Kinematic;
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

        // ✅ Switch back to Dynamic so physics works when idle
        rb.bodyType = RigidbodyType2D.Dynamic;
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
        // Boss now reacts to knockback if Dynamic
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = knockback;
            Debug.Log("BossBunny hit by player and knocked back!");
        }
        else
        {
            Debug.Log("BossBunny hit during jump (no knockback).");
        }
    }
    bool TryAutoFindPlatforms()
    {
        // 1) 컨테이너 우선
        var container = GameObject.Find(platformContainerName);
        var list = new System.Collections.Generic.List<Transform>();

        if (container != null)
        {
            foreach (var t in container.GetComponentsInChildren<Transform>(true))
                if (t != container.transform) list.Add(t);
        }

        // 2) 태그
        if (list.Count == 0 && !string.IsNullOrEmpty(platformTag))
        {
            var tagged = GameObject.FindGameObjectsWithTag(platformTag);
            foreach (var go in tagged) list.Add(go.transform);
        }

        // 3) 이름 검색
        if (list.Count == 0)
        {
            foreach (var t in FindObjectsOfType<Transform>())
            {
                string n = t.name;
                if (n.Contains(platformNameContainsA) || n.Contains(platformNameContainsB))
                    list.Add(t);
            }
        }

        // 결과 적용(일관성 위해 x좌표 기준 정렬)
        if (list.Count > 0)
        {
            platforms = list.OrderBy(tr => tr.position.x).ToArray();
            Debug.Log($"BossBunny: auto-found {platforms.Length} platform points.");
            return true;
        }
        return false;
    }
}

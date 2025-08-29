using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossBunny : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] platforms; // 비워두면 자동 탐색
    [SerializeField] private Transform throwPoint;
    [SerializeField] private GameObject bombPrefab;

    [Header("Auto-Discover Platforms")]
    [SerializeField] private string platformContainerName = "BossPlatforms";
    [SerializeField] private string platformTag = "BossPlatform";
    [SerializeField] private string platformNameContainsA = "BossPlatform";
    [SerializeField] private string platformNameContainsB = "PlatformPoint";
    [SerializeField] private float platformScanTimeout = 5f; // 실시간 기준

    [Header("Jump Settings")]
    [SerializeField] private float jumpAirTime = 1.4f;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float jumpCooldown = 2f;

    [Header("Bomb Settings")]
    [SerializeField] private float bombPrepTime = 0.5f;
    [SerializeField] private int minBombs = 2;
    [SerializeField] private int maxBombs = 6;
    [SerializeField] private float throwHeight = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // state
    private Rigidbody2D rb;
    private Collider2D bossCol;
    private Transform player;
    private Transform currentPlatform;
    private bool onPlatform = true;
    private bool isJumping = false;
    private bool isInvulnerable = false;

    private Coroutine setupRoutine;
    private Coroutine loopRoutine;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        bossCol = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; // 대기 중에는 물리 받도록
    }

    void OnEnable()
    {
        StopAllCoroutines();
        setupRoutine = StartCoroutine(SetupAndRun());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        // 안전 복구
        if (bossCol) bossCol.enabled = true;
        if (rb) rb.bodyType = RigidbodyType2D.Dynamic;
    }

    IEnumerator SetupAndRun()
    {
        // 1) 플랫폼 자동 탐색(실시간 타임아웃 + 매 프레임 재시도)
        float start = Time.realtimeSinceStartup;
        while (!TryAutoFindPlatforms())
        {
            if (Time.realtimeSinceStartup - start > platformScanTimeout)
            {
                if (debugLogs) Debug.LogError("BossBunny: platform scan TIMEOUT. Disabling AI.");
                enabled = false;
                yield break;
            }
            yield return null; // 다음 프레임
        }

        if (platforms == null || platforms.Length == 0)
        {
            if (debugLogs) Debug.LogError("BossBunny: no platform points found.");
            enabled = false;
            yield break;
        }

        // 2) 시작 플랫폼 워프 (하나뿐이면 그대로)
        currentPlatform = platforms[platforms.Length == 1 ? 0 : Random.Range(0, platforms.Length)];
        transform.position = currentPlatform.position;
        onPlatform = true;

        // 3) 플레이어 참조 확보(최대 3초 실시간 재시도)
        float pStart = Time.realtimeSinceStartup;
        while (player == null && Time.realtimeSinceStartup - pStart < 3f)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            yield return null;
        }

        // 4) 메인 루프
        loopRoutine = StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        while (enabled)
        {
            // 일시정지 중에는 대기
            yield return new WaitWhile(() => Time.timeScale == 0f);

            // 플랫폼 위에서, 점프 중 아님, 수직 속도 거의 없음
            yield return new WaitUntil(() => onPlatform && !isJumping && Mathf.Abs(rb.velocity.y) < 0.01f);

            // 다음 목적 플랫폼 선택(동일 플랫폼밖에 없으면 그대로)
            Transform target = GetRandomPlatformExcludingCurrent();

            yield return MoveToPlatform(target);

            // 폭탄 던지기(일시정지 감안)
            int bombs = Mathf.Clamp(Random.Range(minBombs, maxBombs + 1), 0, 99);
            for (int i = 0; i < bombs; i++)
            {
                // pause 중엔 진행 안 함
                yield return new WaitWhile(() => Time.timeScale == 0f);
                yield return new WaitForSeconds(bombPrepTime);
                ThrowBombSafe();
            }

            yield return new WaitWhile(() => Time.timeScale == 0f);
            yield return new WaitForSeconds(jumpCooldown);
        }
    }

    Transform GetRandomPlatformExcludingCurrent()
    {
        if (platforms == null || platforms.Length == 0) return currentPlatform;
        if (platforms.Length == 1) return platforms[0];

        // 다른 거 뽑되, 실패하면 그냥 현재 유지
        for (int tries = 0; tries < 8; tries++)
        {
            var p = platforms[Random.Range(0, platforms.Length)];
            if (p != currentPlatform) return p;
        }
        return currentPlatform;
    }

    IEnumerator MoveToPlatform(Transform target)
    {
        if (target == null)
            yield break;

        isJumping = true;
        isInvulnerable = true;
        onPlatform = false;

        rb.bodyType = RigidbodyType2D.Kinematic;
        bossCol.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos   = target.position;
        float timer = 0f;

        while (timer < jumpAirTime)
        {
            // pause 중에는 advance 하지 않음
            if (Time.timeScale == 0f)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / jumpAirTime);

            float x = Mathf.Lerp(startPos.x, endPos.x, t);
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

        bossCol.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    void ThrowBombSafe()
    {
        // 프리팹/포인트/플레이어 가드
        if (bombPrefab == null || throwPoint == null)
        {
            if (debugLogs) Debug.LogWarning("BossBunny: missing bombPrefab or throwPoint.");
            return;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return; // 플레이어 없으면 던지지 않음
        }

        Vector2 start  = throwPoint.position;
        Vector2 target = player.position;

        Vector2 v = CalculateParabolaVelocity(start, target, throwHeight);

        GameObject bombObj = Instantiate(bombPrefab, start, Quaternion.identity);
        if (bombObj == null) return;

        // Bomb 스크립트가 있으면 주입, 없으면 Rigidbody2D로 기본 속도만
        var bomb = bombObj.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.SetVelocity(v);
            bomb.SetTarget(target);
        }
        else
        {
            var r2d = bombObj.GetComponent<Rigidbody2D>();
            if (r2d != null) r2d.velocity = v;
        }
    }

    Vector2 CalculateParabolaVelocity(Vector2 start, Vector2 target, float height)
    {
        float dy = target.y - start.y;
        float dx = target.x - start.x;

        float safeH = Mathf.Max(height, dy + 0.5f);
        float g = 9.81f;

        float tUp   = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * safeH / g));
        float tDown = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * Mathf.Max(0.01f, safeH - dy) / g));
        float T     = tUp + tDown;

        float vx = dx / Mathf.Max(0.0001f, T);
        float vy = Mathf.Sqrt(2f * g * safeH);
        return new Vector2(vx, vy);
    }

    public void OnHitByPlayer(Vector2 knockback)
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.velocity = knockback; // 빌드 호환성 위해 velocity 사용
            if (debugLogs) Debug.Log("BossBunny: knocked back.");
        }
        else
        {
            if (debugLogs) Debug.Log("BossBunny: hit during scripted jump (no knockback).");
        }
    }

    bool TryAutoFindPlatforms()
    {
        var list = new List<Transform>();

        // 1) 컨테이너 자식
        var container = GameObject.Find(platformContainerName);
        if (container != null)
        {
            foreach (var t in container.GetComponentsInChildren<Transform>(true))
                if (t != container.transform) list.Add(t);
        }

        // 2) 태그
        if (list.Count == 0 && !string.IsNullOrEmpty(platformTag))
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(platformTag))
                list.Add(go.transform);
        }

        // 3) 이름 포함
        if (list.Count == 0)
        {
            foreach (var t in FindObjectsOfType<Transform>(true))
            {
                string n = t.name;
                if (n.Contains(platformNameContainsA) || n.Contains(platformNameContainsB))
                    list.Add(t);
            }
        }

        // 정리: 자기 자신/중복 제거 + x좌표 기준 정렬
        list = list.Where(t => t && t != transform)
                   .Distinct()
                   .OrderBy(t => t.position.x)
                   .ToList();

        // 결과 적용
        if (list.Count > 0)
        {
            platforms = list.ToArray();
            if (debugLogs) Debug.Log($"BossBunny: auto-found {platforms.Length} platforms.");
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (platforms == null) return;
        Gizmos.color = Color.magenta;
        foreach (var p in platforms)
        {
            if (!p) continue;
            Gizmos.DrawSphere(p.position, 0.1f);
        }
    }
#endif
}
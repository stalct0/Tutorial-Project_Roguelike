using UnityEngine;

public class WalletAI : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 1.2f;
    [SerializeField] float patrolEdgeOffset = 0.3f; // 발끝에서 이만큼 앞에서 바닥 확인
    [SerializeField] float edgeCheckDownDist = 0.5f;

    [Header("Ground / Enemy Layers")]
    [SerializeField] LayerMask groundLayer = 0;         // Ground(와 필요시 OneWayPlatform) 포함
    [SerializeField] LayerMask enemyLayerMask = 0;       // Enemy 레이어

    [Header("Front Check")]
    [SerializeField] float frontCheckOffset = 0.3f;      // 몸 중심에서 살짝 앞
    [SerializeField] float frontCheckDist = 0.1f;        // 전방 레이 길이
    [SerializeField] float sameLevelYTolerance = 0.6f;   // 높이 차 허용(같은 층만 충돌로 간주)

    // 캐시
    Rigidbody2D rb;
    Collider2D  col;
    Animator animator;
    SpriteRenderer sr;
    EnemyCombat combat;

    float currentDirection = 1f; // +1 오른쪽, -1 왼쪽

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        combat = GetComponent<EnemyCombat>();

        rb.freezeRotation = true;
    }

    void Update()
    {
        // 맞아서 스턴/발사 중이면 중력/물리에 맡기고 이동 중지
        bool stunnedOrLaunched = (combat != null) && (combat.IsStunned || combat.IsLaunched);
        if (animator) animator.SetBool("isStunned", stunnedOrLaunched);
        if (animator) animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

        if (stunnedOrLaunched) return;

        PatrolMove();
        UpdateFacing();
    }

    void PatrolMove()
    {
        float dir = Mathf.Sign(currentDirection);

        // 1) 전방에 다른 Enemy 있으면 반전 (SojuThrower의 전방 체크 로직 재사용)
        if (IsEnemyInFront(dir))
        {
            currentDirection = -currentDirection;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // 2) 발끝 앞의 바닥이 끊기면 반전 (SojuThrower의 엣지 체크 방식)
        if (!HasGroundAhead(dir))
        {
            currentDirection = -currentDirection;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // 3) 그 외엔 좌우로 일정 속도 유지
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    bool HasGroundAhead(float dir)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * patrolEdgeOffset, 0f);
        // groundLayer가 비어있다면 마지막 안전장치로 "Ground" 이름을 시도
        int mask = groundLayer.value != 0 ? groundLayer.value : LayerMask.GetMask("Ground");
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDownDist, mask);
        Debug.DrawRay(origin, Vector2.down * edgeCheckDownDist, Color.red, 0.1f);
        // 자기 자신 아닌 어떤 바닥이라도 맞으면 OK
        return hit.collider != null && hit.collider.attachedRigidbody != rb;
    }

    bool IsEnemyInFront(float dir)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * frontCheckOffset, 0f);
        Vector2 dirVec = new Vector2(dir, 0f);
        var hit = Physics2D.Raycast(origin, dirVec, frontCheckDist, enemyLayerMask);
        Debug.DrawRay(origin, dirVec * frontCheckDist, Color.cyan, 0.1f);

        if (hit.collider == null) return false;
        if (hit.collider.attachedRigidbody == rb) return false;

        // 같은 층(높이)만 적으로 간주
        float myY = col ? col.bounds.center.y : transform.position.y;
        float otherY = hit.collider.bounds.center.y;
        if (Mathf.Abs(myY - otherY) > sameLevelYTolerance) return false;

        return true;
    }

    void UpdateFacing()
    {
        if (!sr) return;
        // 아트 기준에 맞춰 부호 뒤집어도 됨
        sr.flipX = (currentDirection > 0f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.2f, 0.2f, 0.35f);
        Vector3 a = transform.position + new Vector3(currentDirection * patrolEdgeOffset, 0, 0);
        Gizmos.DrawLine(a, a + Vector3.down * edgeCheckDownDist);
    }
}
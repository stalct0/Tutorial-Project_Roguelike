using System;
using UnityEngine;

public class FlyingPaperAI : MonoBehaviour
{
     [Header("Target & Movement")]
    public Transform player;
    [NonSerialized] private float flySpeed   = 1.5f;   // 일정 비행 속도
    [NonSerialized] private float agroRange  = 6f;     // 추적 시작 거리
    [NonSerialized] private bool  chaseForever = true; // 한번 시작하면 끝까지 추적

    [Header("Visuals/Animation (SojuThrower와 동일 파라미터)")]
    [SerializeField] private string speedParam    = "Speed";
    [SerializeField] private string stunnedParam  = "isStunned";

    public EnemyState currentState = EnemyState.Patrol; // Patrol=대기, MoveToRange=추적, Stun=멈춤
    
    [Header("Gravity")]
    [NonSerialized] float gravityWhileFlying = 0f;   // 평소 비행(중력 없음)
    [NonSerialized] float gravityWhenHit     = 0.8f; // 피격/발사 중 포물선 만들 중력
    // 캐시
    private Rigidbody2D   rb;
    private Animator      animator;
    private SpriteRenderer sr;
    private EnemyCombat   combat;

    // 내부 상태
    private bool hasChased = false;     // 한번이라도 추적 시작했는지
    private Vector2 lastDir = Vector2.right; // 플레이어가 null일 때 계속 유지할 방향

    void Start()
    {
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr       = GetComponent<SpriteRenderer>();
        combat   = GetComponent<EnemyCombat>();

        // 날아다님: 회전고정 + 중력 0
        rb.freezeRotation = true;
        rb.gravityScale   = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
            // ★ CHG: 해킹 스턴 vs 발사 분리
                bool hackedStun = (combat != null) && combat.IsStunned && !combat.IsLaunched;
            bool launched   = (combat != null) && combat.IsLaunched;
            if (animator) animator.SetBool(stunnedParam, hackedStun || launched);
        
                if (hackedStun)
                {
                    // ★ ADD: 해킹 스턴 → 제자리 정지(속도 0) + 중력 0
                        currentState     = EnemyState.Stun;
                    rb.gravityScale  = gravityWhileFlying;
                    rb.linearVelocity= Vector2.zero;
                    // 상태머신 아래 로직 타지 않도록
                        if (animator) animator.SetFloat(speedParam, 0f);
                    UpdateFacing();
                    return;
                }
            else if (launched)
                {
                    // ★ ADD: 발사(넉백) → 중력만 켜서 포물선, 속도는 유지
                        currentState    = EnemyState.Stun;
                    rb.gravityScale = gravityWhenHit;
                    if (animator) animator.SetFloat(speedParam, rb.linearVelocity.magnitude);
                    UpdateFacing();
                    return;
                }
            else if (currentState == EnemyState.Stun)
                {
                    // ★ CHG: 스턴 해제 → 이전 정책대로 복귀
                        currentState = hasChased ? EnemyState.MoveToRange : EnemyState.Patrol;
                }

        
        
        if (animator) animator.SetFloat(speedParam, rb.linearVelocity.magnitude);
        UpdateFacing();

        switch (currentState)
        {
            case EnemyState.Patrol:
                IdlePatrol();
                break;
            case EnemyState.MoveToRange:
                ChaseForever();
                break;
            case EnemyState.Stun:

                bool hitOrLaunched = (combat != null) && (combat.IsStunned || combat.IsLaunched);
                rb.gravityScale = hitOrLaunched ? gravityWhenHit : gravityWhileFlying;
                if (hitOrLaunched)
                {
                    currentState = EnemyState.Stun;   // 이미 이렇게 쓰고 있다면 중복 OK
                    // 여기서 return하면 chase 코드가 y속도를 덮어쓰지 않음
                    return;
                }
                break;
        }
    }

    void IdlePatrol()
    {
        // 대기: 가만히 있다가 범위에 들어오면 추적 시작
        rb.linearVelocity = Vector2.zero;
        if (!player) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (!float.IsNaN(dist) && dist <= agroRange)
        {
            hasChased = true;
            currentState = EnemyState.MoveToRange;
        }
    }

    void ChaseForever()
    {
        // 한번 들어오면 죽을 때까지 끊기지 않고 플레이어 방향으로 직진
        Vector2 dir;
        if (player != null)
            dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        else
            dir = lastDir; // 혹시 플레이어 오브젝트가 일시적으로 없으면 마지막 방향 유지

        if (dir.sqrMagnitude > 0.0001f)
            lastDir = dir;

        rb.linearVelocity = dir * flySpeed;

        // chaseForever=false면, (원한다면) 멀어졌을 때 패트롤로 복귀시키는 로직을 추가 가능
        // 여기서는 요청대로 '영구 추적'이 기본값
    }

    void UpdateFacing()
    {
        // 이동 방향 기준 좌우 반전 (아트 기준에 따라 부호 뒤집어도 됨)
        if (sr)
        {
            float vx = rb.linearVelocity.x;
            if (Mathf.Abs(vx) > 0.01f)
                sr.flipX = (vx > 0f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 추적 시작 거리 시각화
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, agroRange);
    }
}

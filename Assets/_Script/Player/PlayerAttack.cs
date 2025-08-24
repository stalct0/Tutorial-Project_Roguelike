using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 다운스윙(위→아래) 근접공격을 OverlapBox 3지점 스윕으로 판정
/// - 공격키를 누르면 액티브 구간 동안 상/중/하 3점을 순차적으로 검사
/// - 한 스윙 동안 같은 대상을 중복 히트하지 않음
/// - 적에게는 IHittable.TakeHit(DamageEvent)로 통지
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerAttack : MonoBehaviour
{
 
    private KeyCode attackKey = KeyCode.J; // 필요시 New Input System으로 교체
    
    public LayerMask enemyLayer;
    public LayerMask projectileLayer;
    
    private int baseDamage = 10;
    private float knockbackForce = 6f;
    private float stunSeconds = 0.4f;
    private float cooldown = 0.7f;
    private float attackDelay = 0.2f;
    
    private float activeTime = 0.3f;
    private int sweepPoints = 3;
    
    private Vector2 boxSize = new Vector2(0.6f, 0.5f);
    private Vector2 topOffset = new Vector2(0.25f, 0.2f);
    private Vector2 bottomOffset = new Vector2(0.25f, -0.2f);
    
    private bool flipByLocalScaleX = true;

    [Header("디버그")]
    [SerializeField] [ReadOnly] private bool drawGizmos = true;

    // 내부 상태
    private bool isAttacking = false;
    private float lastAttackTime = -999f;
    private readonly HashSet<int> hitVictims = new();
    private PlayerStats pstats;

    private Animator animator;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        pstats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        // 쿨타임/공격 중이면 무시
        if (isAttacking || Time.time - lastAttackTime < cooldown) return;

        // 간단 키 입력 (원하면 New Input System으로 대체)
        if (Input.GetKeyDown(attackKey))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hitVictims.Clear();
        
        if (animator) animator.SetTrigger("Attack");
        
        yield return new WaitForSeconds(attackDelay);
        
        // 액티브 구간을 sweepPoints 등분해서 순차 검사
        int steps = Mathf.Max(3, sweepPoints);              // 최소 3 보장
        float stepWait = activeTime / (steps - 1);          // 3포인트면 0, 0.5, 1.0 타이밍
        for (int i = 0; i < steps; i++)
        {
            float t = (steps == 1) ? 1f : i / (float)(steps - 1); // 0→1
            Vector2 localOffset = Vector2.Lerp(topOffset, bottomOffset, t);

            // 좌우 반전 처리(캐릭터 localScale.x로 판단)
            if (flipByLocalScaleX && transform.localScale.x < 0f)
                localOffset.x = -localOffset.x;

            // 로컬 오프셋 → 월드 중심 좌표 (우측/상단 기준 단순 투영)
            Vector2 center =
                (Vector2)transform.position
                + (Vector2)transform.right * localOffset.x
                + Vector2.up * localOffset.y;

            // 판정
            DoHitbox(center, boxSize);

            // 다음 포인트로
            if (i < steps - 1)
                yield return new WaitForSeconds(stepWait);
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    void DoHitbox(Vector2 center, Vector2 size)
    {
        int mask = enemyLayer.value | projectileLayer.value; // 두 레이어 모두 검색
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, mask);
        if (hits == null || hits.Length == 0) return;

        int finalDamage = pstats.currentAttackDamage + baseDamage;
        Debug.Log($"final damage: {finalDamage}");

        foreach (var col in hits)
        {
            if (col) continue;

            // IHittable이면 통일 처리: 적이든 소주병이든 TakeHit 호출
            if (col.TryGetComponent<IHittable>(out var hittable) ||
                col.GetComponentInParent<IHittable>() is IHittable parentHittable && (hittable = parentHittable) != null)
            {
                // 한 스윙 중 중복 히트 방지
                int id = col.GetInstanceID();
                if (!hitVictims.Add(id)) continue;

                var ev = new DamageEvent
                {
                    damage = finalDamage,
                    sourcePos = transform.position,
                    knockbackForce = knockbackForce,
                    stunSec = stunSeconds,
                    instigator = this.gameObject
                };
                hittable.TakeHit(ev);
            }
        }
    }

    // 디버그 기즈모(상/중/하 박스)
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // 미리 위치 계산용(에디터에서도 대략 보여주기)
        Vector2 top = topOffset, mid = Vector2.Lerp(topOffset, bottomOffset, 0.5f), bot = bottomOffset;

        if (flipByLocalScaleX && transform.localScale.x < 0f)
        {
            top.x = -top.x; mid.x = -mid.x; bot.x = -bot.x;
        }

        DrawBoxGizmo(top, Color.cyan);
        DrawBoxGizmo(mid, Color.yellow);
        DrawBoxGizmo(bot, Color.red);
    }

    void DrawBoxGizmo(Vector2 localOffset, Color color)
    {
        Gizmos.color = color;
        Vector3 c = transform.position
                    + transform.right * localOffset.x
                    + Vector3.up * localOffset.y;
        Gizmos.DrawWireCube(c, (Vector3)boxSize);
    }
}
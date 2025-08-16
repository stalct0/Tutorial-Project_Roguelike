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
    [Header("입력")]
    [SerializeField] private KeyCode attackKey = KeyCode.J; // 필요시 New Input System으로 교체

    [Header("타겟 레이어")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("공격 스펙")]
    [Tooltip("기본 공격력(여기에 PlayerStats.attackDamage를 더해 최종 대미지를 계산)")]
    [SerializeField] [ReadOnly] private int baseDamage = 10;
    [SerializeField] [ReadOnly] private float knockbackForce = 6f;
    [SerializeField] [ReadOnly] private float stunSeconds = 0.4f;
    [SerializeField] [ReadOnly] private float cooldown = 0.35f;

    [Header("액티브 프레임/스윕")]
    [Tooltip("액티브(유효) 시간 전체 길이")]
    [SerializeField] [ReadOnly] private float activeTime = 0.15f;
    [Tooltip("스윕 포인트 개수(권장 3). 3이면 상/중/하 순서로 검사")]
    [SerializeField] [ReadOnly] private int sweepPoints = 3;

    [Header("히트박스 설정(로컬 기준)")]
    [Tooltip("OverlapBox의 월드 크기")]
    [SerializeField] [ReadOnly] private Vector2 boxSize = new Vector2(1.0f, 0.8f);
    [Tooltip("스윙 시작(위쪽) 로컬 오프셋")]
    [SerializeField] [ReadOnly] private Vector2 topOffset = new Vector2(0.7f, 1.0f);
    [Tooltip("스윙 끝(아래쪽) 로컬 오프셋")]
    [SerializeField] [ReadOnly] private Vector2 bottomOffset = new Vector2(0.7f, 0.1f);
    [Tooltip("로컬 X 방향을 좌우 반전할지(캐릭터 좌우 반전 시 사용)")]
    [SerializeField] [ReadOnly] private bool flipByLocalScaleX = true;

    [Header("디버그")]
    [SerializeField] [ReadOnly] private bool drawGizmos = true;

    // 내부 상태
    private bool isAttacking = false;
    private float lastAttackTime = -999f;
    private readonly HashSet<int> hitVictims = new();
    private PlayerStats pstats;

    void Awake()
    {
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
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        if (hits == null || hits.Length == 0) return;

        int finalDamage = (pstats != null ? pstats.attackDamage : 0) + baseDamage;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // 한 스윙에서 같은 대상 중복 히트 방지
            int id = col.GetInstanceID();
            if (!hitVictims.Add(id)) continue;

            // IHittable에 대미지 통지
            if (col.TryGetComponent<IHittable>(out var hittable) ||
                col.GetComponentInParent<IHittable>() is IHittable parentHittable && (hittable = parentHittable) != null)
            {
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
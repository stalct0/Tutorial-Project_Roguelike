using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 소프트웨어 캐릭터 전용:
/// - 근접 휘두르기(OverlapBox 3지점 스윕, 넉백 0, 스턴 O)
/// - 공격 종료 타이밍에 투사체 발사
[RequireComponent(typeof(PlayerStats))]
public class SoftwareAttack : MonoBehaviour
{
    [SerializeField] private KeyCode attackKey = KeyCode.J;
     private float attackDelay = 0.2f;     
     private float activeDuration = 0.3f;  
     private float cooldown = 0.7f;       

    [SerializeField] private LayerMask hittableMask;
    private Vector2 boxSize = new Vector2(0.6f, 0.5f);
    private Vector2 top = new Vector2(0.25f, 0.2f);
    private Vector2 mid = new Vector2(0.25f, 0f);
    private Vector2 bot = new Vector2(0.25f, -0.2f);
     
     private int   damageBonus = 1;         
     private float meleeStunSeconds = 0.4f;
     private bool  drawGizmos = true;

    [Header("Projectile (발사 타이밍 = 근접 끝)")]
    [SerializeField] private HackProjectile projectilePrefab;
    private Vector2 projectileSpawnOffset = new Vector2(0.2f, 0.1f);
    [SerializeField] private float projectileDamageScale = 1f;

    private Animator animator;
    private string   animTriggerAttack = "Attack";

    private PlayerStats pstats;
    private bool isAttacking;
    private float lastAttackTime;

    void Awake()
    {
        pstats = GetComponent<PlayerStats>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 혹시 잘못 붙었을 때 방지(선택)
        if (GameManager.Instance &&
            GameManager.Instance.SelectedClass != PlayerClass.Software) return;

        if (isAttacking) return;
        if (Time.time - lastAttackTime < cooldown) return;

        if (Input.GetKeyDown(attackKey))
            StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator && !string.IsNullOrEmpty(animTriggerAttack))
            animator.SetTrigger(animTriggerAttack);

        // 바람잡이
        if (attackDelay > 0f) yield return new WaitForSeconds(attackDelay);

        // 액티브 구간: 위 → 중 → 아래 순으로 3회 스윕
        float step = activeDuration / 3f;

        DoHitbox(top);
        if (step > 0f) yield return new WaitForSeconds(step);

        DoHitbox(mid);
        if (step > 0f) yield return new WaitForSeconds(step);

        DoHitbox(bot);
        // 스윕이 끝난 "지금"이 공격 종료 타이밍 → 투사체 발사
        FireProjectile();

        isAttacking = false;
    }

    void DoHitbox(in Vector2 localOffset)
    {
        // 좌우 방향 반영
        float dirX = Mathf.Sign(transform.localScale.x);
        Vector2 center =
            (Vector2)transform.position
            + (Vector2)transform.right * (localOffset.x * dirX)   // 좌우 반전
            + Vector2.up * localOffset.y;

        // OverlapBoxAll
        var hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, hittableMask);
        if (hits == null || hits.Length == 0) return;

        int damage = Mathf.Max(0, pstats.currentAttackDamage + damageBonus);

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            var hittable = col.GetComponent<IHittable>();
            if (hittable == null) hittable = col.GetComponentInParent<IHittable>();
            if (hittable == null) continue;

            var ev = new DamageEvent
            {
                damage = damage,
                sourcePos = transform.position,
                knockbackForce = 0f,               // ★ 핵심: 근접 넉백 없음
                stunSec = meleeStunSeconds,
                instigator = this.gameObject
            };
            hittable.TakeHit(ev);
        }
    }

    void FireProjectile()
    {
        if (!projectilePrefab) return;

        float dirX = Mathf.Sign(transform.localScale.x);

        // 로컬 오프셋으로 스폰 위치 계산 (자식 Transform 필요 없음)
        Vector3 spawnPos =
            transform.position
            + transform.right * (projectileSpawnOffset.x * dirX)
            + Vector3.up * projectileSpawnOffset.y;

        var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        proj.damage = Mathf.RoundToInt(pstats.currentAttackDamage * projectileDamageScale);
        proj.Fire(new Vector2(dirX, 0f));
    }

    // 디버그 기즈모
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        DrawBoxGizmo(top, Color.cyan);
        DrawBoxGizmo(mid, Color.yellow);
        DrawBoxGizmo(bot, Color.red);
        // Projectile spawn position
        Gizmos.color = new Color(0.2f, 1f, 1f, 0.35f);
        Vector3 ps = transform.position
                    + transform.right * (projectileSpawnOffset.x * Mathf.Sign(transform.localScale.x))
                    + Vector3.up * projectileSpawnOffset.y;
        Gizmos.DrawSphere(ps, 0.08f);
    }

    void DrawBoxGizmo(Vector2 localOffset, Color color)
    {
        Gizmos.color = color;
        Vector3 c = transform.position
                    + transform.right * (localOffset.x * Mathf.Sign(transform.localScale.x))
                    + Vector3.up * localOffset.y;
        Gizmos.DrawWireCube(c, (Vector3)boxSize);
    }
}
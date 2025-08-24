using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 발사(Launched) 상태일 때만 켜져서
/// "날아가는 적"이 다른 적과 부딪히면 2차 대미지를 주는 캐리어 트리거.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyHurtCarrier : MonoBehaviour
{

    [NonSerialized] public int baseDamage = 8;
    [NonSerialized] public float knockbackForce = 4.5f;
    [NonSerialized] public float stunSec = 0.25f;
    
    [NonSerialized] public bool scaleBySpeed = true;
    [NonSerialized] public float speedToDamage = 0.6f;   // damage += speed * 이 값
    [NonSerialized] public float speedToKnock   = 0.7f;  // knockback += speed * 이 값
    
    [NonSerialized] public int maxHits = 3;              // 한 번 발사에서 최대 몇 번이나 때릴지
    [NonSerialized] public float perVictimCooldown = 0.5f; // 같은 피해자 재히트 쿨다운

    private EnemyCombat owner;
    private Collider2D triggerCol;
    private Rigidbody2D ownerRB;
    private int hitCount;
    private readonly Dictionary<int, float> victimLock = new(); // victimID -> nextAllowedTime
    private bool active;

    void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        triggerCol.isTrigger = true;
        triggerCol.enabled = false; // 시작은 꺼둠
    }

    public void BindOwner(EnemyCombat combat)
    {
        owner = combat;
        ownerRB = combat.GetComponent<Rigidbody2D>();
    }

    public void SetActiveCarrier(bool enable)
    {
        active = enable;
        triggerCol.enabled = enable;
        if (enable)
        {
            hitCount = 0;
            victimLock.Clear();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;
        if (hitCount >= maxHits) return;

        // 자기 자신/자기 루트는 무시
        if (owner != null && (other.transform == owner.transform || other.transform.IsChildOf(owner.transform)))
            return;

        // 상대가 IHittable인가?
        if (!TryGetHittable(other, out var target)) return;

        int vid = other.GetInstanceID();
        if (victimLock.TryGetValue(vid, out float nextOk) && Time.time < nextOk)
            return; // 쿨다운 중

        // 현재 속도 기반으로 피해/넉백 보정
        float speed = (ownerRB != null) ? ownerRB.linearVelocity.magnitude : 0f;
        int   dmg   = baseDamage + (scaleBySpeed ? Mathf.RoundToInt(speed * speedToDamage) : 0);
        float kb    = knockbackForce + (scaleBySpeed ? speed * speedToKnock : 0f);

        Debug.Log(speed);
        int finalDamage = GameManager.Instance.PStats.currentAttackDamage + dmg;
        
        // 넉백 방향: 캐리어 진행 방향
        Vector2 dir = (ownerRB != null && ownerRB.linearVelocity.sqrMagnitude > 0.0001f)
            ? ownerRB.linearVelocity.normalized
            : ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

        var e = new DamageEvent
        {
            damage        = finalDamage,
            sourcePos     = (Vector2)transform.position - dir * 0.1f, // 진행 반대쪽을 소스처럼
            knockbackForce= kb,
            stunSec       = stunSec,
            instigator    = owner != null ? owner.gameObject : this.gameObject
        };

        target.TakeHit(e);

        hitCount++;
        victimLock[vid] = Time.time + perVictimCooldown;

        // 히트 한계 도달 시 자동 비활성
        if (hitCount >= maxHits)
            SetActiveCarrier(false);
    }

    bool TryGetHittable(Collider2D col, out IHittable hittable)
    {
        hittable = col.GetComponent<IHittable>();
        if (hittable != null) return true;

        hittable = col.GetComponentInParent<IHittable>();
        return hittable != null;
    }
}
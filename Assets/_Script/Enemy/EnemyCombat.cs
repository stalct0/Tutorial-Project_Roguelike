using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyCombat : MonoBehaviour, IHittable
{
    [Header("HP")]
    [ReadOnly] public int maxHP = 30;
    [ReadOnly] public int currentHP;

    [Header("방어/무적")]
    [ReadOnly] public float invincibleSecOnHit = 0.15f; // 중복타/폭주 방지
    private bool isInvincible;
    private float invTimer;

    [Header("스턴")]
    [ReadOnly] public float minStunSec = 0.2f;          // 들어온 e.stunSec과 비교해서 더 큰 값 사용
    private bool isStunned;
    private float stunTimer;

    [Header("넉백/발사")]
    [ReadOnly] public float upwardKnockAdd = 0.35f;     // 수직 성분 가산(다운스윙 느낌용)
    [ReadOnly] public float launchMinSpeed = 2.0f;      // 이 속도 미만이면 발사 상태 종료 후보
    [ReadOnly] public float launchMaxDuration = 0.8f;   // 발사 유지 최대 시간
    [ReadOnly] public float stopSpeedThreshold = 0.8f;  // 완전히 멈췄다고 볼 속도
    private bool isLaunched;
    private float launchTimer;

    [Header("연쇄 타격(캐리어)")]
    public EnemyHurtCarrier carrier;         // 자식 오브젝트(Trigger) 참조
    [Tooltip("발사 동안 레이어를 변경하려면 설정(없으면 -1)")]
    [ReadOnly] public int projectileLayer = -1;
    private int originalLayer;

    [Header("이벤트")]
    public UnityEvent onDie;

    // 캐시
    private Rigidbody2D rb;
    private Collider2D  col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        currentHP = maxHP;
        originalLayer = gameObject.layer;

        // 안전장치: 캐리어 없으면 찾아보기(자식에서)
        if (carrier == null)
            carrier = GetComponentInChildren<EnemyHurtCarrier>(includeInactive: true);
        if (carrier != null)
            carrier.BindOwner(this);

        // 빠르게 날아갈 일이 있으니(다운스윙 발사), 터널링 방지
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        // 무적 타이머
        if (isInvincible)
        {
            invTimer -= Time.deltaTime;
            if (invTimer <= 0f) isInvincible = false;
        }

        // 스턴
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                isStunned = false;
        }

        // 발사 상태 유지/종료 판정
        if (isLaunched)
        {
            launchTimer += Time.deltaTime;

            // 속도가 너무 느려졌거나, 최대 시간 경과 시 종료
            if (rb.linearVelocity.magnitude < stopSpeedThreshold || launchTimer > launchMaxDuration)
                EndLaunched();
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    public void TakeHit(DamageEvent e)
    {
        if (currentHP <= 0) return;      // 이미 사망
        if (isInvincible) return;        // i-frame

        // 대미지
        currentHP -= Mathf.Max(0, e.damage);
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
            return;
        }

        // 스턴
        float stunSec = Mathf.Max(minStunSec, e.stunSec);
        StartStun(stunSec);

        // 넉백/발사
        KnockbackFrom(e.sourcePos, e.knockbackForce);

        // 짧은 무적
        SetInvincible(invincibleSecOnHit);
    }

    void StartStun(float sec)
    {
        isStunned = true;
        stunTimer = sec;

        // 여기서 AI/이동을 정지하고 싶다면 해당 컴포넌트를 disable 하거나,
        // 이동 로직에서 isStunned를 체크해 입력/추적을 무시하세요.
    }

    void KnockbackFrom(Vector2 sourcePos, float force)
    {
        // 공격자 반대 방향으로 날아가게 (다운스윙 느낌 위해 y 가산)
        Vector2 dir = ((Vector2)transform.position - sourcePos).normalized;
        dir.y += upwardKnockAdd;
        dir.Normalize();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * force, ForceMode2D.Impulse);

        // 충분히 튕겨나가면 "발사" 상태로 간주 (캐리어 활성)
        if (rb.linearVelocity.magnitude >= launchMinSpeed)
            BeginLaunched();
    }

    void BeginLaunched()
    {
        if (isLaunched) return;
        isLaunched = true;
        launchTimer = 0f;

        // 전용 레이어 사용 시 전환(플레이어/지형과의 원치 않는 상호작용 차단에 도움)
        if (projectileLayer >= 0)
            gameObject.layer = projectileLayer;

        if (carrier != null)
            carrier.SetActiveCarrier(true);
    }

    void EndLaunched()
    {
        if (!isLaunched) return;
        isLaunched = false;

        if (carrier != null)
            carrier.SetActiveCarrier(false);

        // 레이어 복귀
        gameObject.layer = originalLayer;
    }

    void SetInvincible(float sec)
    {
        isInvincible = true;
        invTimer = sec;
    }

    void Die()
    {
        // 캐리어 끄기
        if (carrier != null) carrier.SetActiveCarrier(false);

        onDie?.Invoke();
        Destroy(gameObject);
    }

    // 벽/지형에 크게 박으면 즉시 종료하고 싶을 때(선택)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isLaunched)
        {
            // 강하게 충돌했으면 멈춘 걸로 처리
            if (collision.relativeVelocity.magnitude > 1.5f)
                EndLaunched();
        }
    }

    // 외부에서 현재 스턴/발사 상태를 필요하면 읽을 수 있게
    public bool IsStunned => isStunned;
    public bool IsLaunched => isLaunched;
}
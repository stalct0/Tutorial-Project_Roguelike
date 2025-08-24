using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyCombat : MonoBehaviour, IHittable
{
    
    [NonSerialized] public int maxHP = 30;
    [NonSerialized]public int currentHP;


    [NonSerialized] public float invincibleSecOnHit = 0.15f; // 중복타/폭주 방지
    private bool isInvincible;
    private float invTimer;


    [NonSerialized] public float minStunSec = 0.2f;          // 들어온 e.stunSec과 비교해서 더 큰 값 사용
    private bool isStunned;
    private float stunTimer;

    [NonSerialized] public float upwardKnockAdd = 0.35f;     // 수직 성분 가산(다운스윙 느낌용)
    [NonSerialized] public float launchMinSpeed = 2.0f;      // 이 속도 미만이면 발사 상태 종료 후보
    [NonSerialized] public float launchMaxDuration = 0.8f;   // 발사 유지 최대 시간
    [NonSerialized] public float stopSpeedThreshold = 0.8f;  // 완전히 멈췄다고 볼 속도
    private bool isLaunched;
    private float launchTimer;

    private bool deathLaunch = true;        // 죽을 때도 발사로 날아가게 할지
    private float deathKnockMultiplier = 1.0f; // 죽음 시 넉백 보정
    private float deathMaxAirTime = 0.8f;   // 최대 비행 대기시간(세이프티)
    
    [Header("연쇄 타격(캐리어)")]
    public EnemyHurtCarrier carrier;         // 자식 오브젝트(Trigger) 참조
    
    [Tooltip("발사 동안 레이어를 변경하려면 설정(없으면 -1)")]
    [ReadOnly] public int projectileLayer = -1;
    private int originalLayer;

    [Header("이벤트")]
    public UnityEvent onDie;

    [Header("Visuals (Hit Flash)")]
    [SerializeField] private SpriteRenderer spriteRenderer;   // 비워두면 자동 할당
    [SerializeField] private float hitFlashDuration = 0.05f;  // 상자와 동일 0.05s
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.6f, 0.6f, 1f);
    
    // 캐시
    private Rigidbody2D rb;
    private Collider2D  col;
    private Coroutine _hitFlashCo;    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        maxHP = Mathf.RoundToInt(maxHP * GameManager.Instance.LevelCoefficient);
        
        currentHP = maxHP;
        originalLayer = gameObject.layer;

        // 안전장치: 캐리어 없으면 찾아보기(자식에서)
        if (carrier == null)
            carrier = GetComponentInChildren<EnemyHurtCarrier>(includeInactive: true);
        if (carrier != null)
            carrier.BindOwner(this);

        // 빠르게 날아갈 일이 있으니(다운스윙 발사), 터널링 방지
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        
        if (spriteRenderer)
        {
            if (_hitFlashCo != null) StopCoroutine(_hitFlashCo);
            _hitFlashCo = StartCoroutine(HitFlashCo());
        }
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            if (deathLaunch)
            {
                // 더 맞지 않도록 사실상 무적·피격무시
                isInvincible = true;

                // 죽음 넉백 + 발사 시작
                DeathLaunch(e);

                // 발사 끝날 때까지 기다렸다가 파괴
                StartCoroutine(Co_DestroyAfterDeathLaunch());
            }
            else
            {
                Die();
            }
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

    void DeathLaunch(DamageEvent e)
    {
        // 넉백 방향 계산(기존 KnockbackFrom 로직과 동일)
        Vector2 dir = ((Vector2)transform.position - e.sourcePos).normalized;
        dir.y += upwardKnockAdd;
        dir.Normalize();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * (e.knockbackForce * deathKnockMultiplier), ForceMode2D.Impulse);

        // 충분히 빨라지면 발사 시작(캐리어 on)
        if (rb.linearVelocity.magnitude >= launchMinSpeed)
            BeginLaunched();
        else
            BeginLaunched(); // 느려도 캐리어를 잠깐 켜고 싶다면 강제 on (필요 없으면 삭제)
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

    public void Die()
    {
        // 캐리어 끄기
        if (carrier != null) carrier.SetActiveCarrier(false);
        
        onDie?.Invoke();
        GameManager.Instance.PStats.AddMoney();
        Destroy(gameObject);
    }

    public void InstaDie()
    {
        onDie?.Invoke();
        GameManager.Instance.PStats.AddMoney();
        Destroy(gameObject);
    }

    IEnumerator Co_DestroyAfterDeathLaunch()
    {
        float t = 0f;

        // 발사 상태가 끝나거나, 속도가 임계 이하이거나, 세이프티 타임아웃에 걸릴 때까지 대기
        while (t < deathMaxAirTime)
        {
            bool doneBySpeed = rb.linearVelocity.magnitude < stopSpeedThreshold;
            if (!isLaunched || doneBySpeed) break;
            t += Time.deltaTime;
            yield return null;
        }

        // 깔끔하게 마무리
        EndLaunched();                 // 캐리어 off & 레이어 복귀
        Die();                         // onDie 이벤트 호출 + Destroy
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
    
    private IEnumerator HitFlashCo()
    {
        Color orig = spriteRenderer.color;
        Color flash = new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, orig.a);
        spriteRenderer.color = flash;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = orig;
        _hitFlashCo = null;
    }
}
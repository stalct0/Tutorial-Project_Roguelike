using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class PlayerStats : MonoBehaviour
{
    public StatDisplay statDisplay;
    
    [NonSerialized] public int startAttackDamage = 10;
    [NonSerialized] public int startMoney = 50;
    [NonSerialized] public int startMoveSpeed = 5;
    [NonSerialized] public int startDashCoolDown = 2;
    
    [ReadOnly] public int maxHealth = 100; // = starthealth
    [ReadOnly] public int currentHealth { get; private set; }
    [ReadOnly] public int currentAttackDamage { get; private set; }
    [ReadOnly] public int currentMoney { get; private set; }
    [ReadOnly] public int currentMoveSpeed { get; private set; }
    [ReadOnly] public int currentDashCoolDown { get; private set; }

    int   _baseMaxHealth;
    int   _baseStartAttack;
    int   _baseStartMoveSpeed;
    int   _baseStartDashCoolDown;

    float _maxAddPassive   = 0f, _maxMulPassive   = 1f;
    float _atkAddPassive   = 0f, _atkMulPassive   = 1f;
    float _spdAddPassive   = 0f, _spdMulPassive   = 1f;
    float _dashAddPassive  = 0f, _dashMulPassive  = 1f;
    
    [SerializeField] private SpriteRenderer[] spriteRenderers;   // 비워두면 자동 할당
    [SerializeField] private float hitFlashDuration = 0.05f;     // 상자와 동일
    [SerializeField] private Color hitFlashColor   = new Color(1f, 0.6f, 0.6f, 1f);
    [SerializeField] private float invincibleBlinkInterval = 0.1f;

    private Coroutine _hitFlashCo;
    private Coroutine _blinkCo;
    
    public bool isInvincible = false;
    private float invincibleTimer = 0f;
    
    
    public UnityEvent onDie;
    public PlayerController controller; 

    //아이템
    private readonly List<StatDelta> _appliedDeltas = new();
    
    void Awake()
    {
        controller = GetComponent<PlayerController>();

        currentHealth = maxHealth;
        currentAttackDamage = startAttackDamage;
        currentMoney = startMoney;
        currentMoveSpeed = startMoveSpeed;
        currentDashCoolDown = startDashCoolDown;
       
        _baseMaxHealth        = maxHealth;
        _baseStartAttack      = startAttackDamage;
        _baseStartMoveSpeed   = startMoveSpeed;
        _baseStartDashCoolDown= startDashCoolDown;

        _maxAddPassive = 0f; _maxMulPassive = 1f;
        _atkAddPassive = 0f; _atkMulPassive = 1f;
        _spdAddPassive = 0f; _spdMulPassive = 1f;
        _dashAddPassive= 0f; _dashMulPassive= 1f;
        
        if (statDisplay != null)
        {
            statDisplay.SetMaxHealth(maxHealth);
            statDisplay.SetAttackDamage(currentAttackDamage);
            statDisplay.SetCurrentMoney(currentMoney);
            statDisplay.SetCurrentMoveSpeed(currentMoveSpeed);
        }
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
        
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(this);
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
                isInvincible = false;
        }
    }
    
    public void TakeDamage(int amount)
    {
        
        if (isInvincible) return;
        
        if (_hitFlashCo != null) StopCoroutine(_hitFlashCo);
        _hitFlashCo = StartCoroutine(HitFlashCo());
        
        int totalDamage = Mathf.RoundToInt(amount * (GameManager.Instance.LevelCoefficient));
        currentHealth -= totalDamage;
        Debug.Log($"damage: {totalDamage}");
        
        if (currentHealth < 0)
            currentHealth = 0;
        
        if (statDisplay != null)
            statDisplay.SetCurrentHealth(currentHealth);
        
        //스턴
        if (controller.shortStunDuration > 0f)
        {
                controller.ShortStun(controller.shortStunDuration);
                SetInvincible(controller.shortStunInvincibleDuration);
        }
        
        if (currentHealth <= 0)
            Die();
    }
    
    public void TakeDamageKnockback(int amount,Vector2? sourcePosition, float knockbackForce)
    {
        if (isInvincible) return;
        
        if (_hitFlashCo != null) StopCoroutine(_hitFlashCo);
        _hitFlashCo = StartCoroutine(HitFlashCo());
        
        int totalDamage = Mathf.RoundToInt(amount * (GameManager.Instance.LevelCoefficient));
        currentHealth -= totalDamage;
        if (currentHealth < 0)
            currentHealth = 0;
        
        Debug.Log($"damage: {totalDamage}");

        if (statDisplay) 
            statDisplay.SetCurrentHealth(currentHealth);
        
        //스턴
        if (controller.shortStunDuration > 0f)
        {
            controller.ShortStun(controller.shortStunDuration);
            controller.KnockbackFrom(sourcePosition.Value, knockbackForce);
            SetInvincible(controller.shortStunInvincibleDuration);
        }
        
        if (currentHealth <= 0)
            Die();
    }
    
    public void InstantDeath()
    {
        currentHealth = 0;
        Die();
    }
    
    public void TakeLongStun(int amount)
    {
        if (controller.longStunDuration > 0f)
        {
            if (controller != null)
                controller.LongStun(controller.longStunDuration);
        }
    }

    public void SetInvincible(float duration)
    {
        isInvincible = true;
        invincibleTimer = duration;
        
        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = StartCoroutine(InvincibleBlinkCo());
    }

    
    private void Die()
    {
        GameObject.Destroy(gameObject);
        if (onDie != null)
            onDie.Invoke();
    }

    public void ResetStats()
    {
        currentHealth = maxHealth;
    }
    
    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (currentMoney < amount) return false;
        currentMoney -= amount;
        statDisplay?.SetCurrentMoney(currentMoney);
        return true;
    }

    public void AddMoney()
    {
        int amount = Random.Range(5, 10);
        if (amount == 0) return;
        currentMoney = Mathf.Max(0, currentMoney + amount);
        statDisplay?.SetCurrentMoney(currentMoney);
    }
    
    //아이템 
    public void ApplyStatDelta(StatDelta d)
{
    switch (d.stat)
    {
        case StatType.MaxHealth:
            if (d.isMultiplier)
            {
                _maxMulPassive *= (d.amount / 100f);
                currentHealth = Mathf.RoundToInt(currentHealth * (d.amount / 100f)); 
            }
            else
            {
                _maxAddPassive += d.amount;                 // ★ CHG
                currentHealth = Mathf.RoundToInt(currentHealth + d.amount);
            }
            RecalcMaxHealth();                              // ★ CHG
            break;

        case StatType.CurrentHealth:
            {
                int initial2 = currentHealth;
                if (d.isMultiplier)
                    currentHealth = Mathf.RoundToInt(currentHealth + (maxHealth * (d.amount / 100f)));
                else
                    currentHealth = Mathf.Max(1, currentHealth + Mathf.RoundToInt(d.amount));
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                currentHealth = Mathf.Max(initial2, currentHealth);
                statDisplay?.SetCurrentHealth(currentHealth);
            }
            break;

        case StatType.CurrentAttackDamage:
            if (d.isMultiplier) _atkMulPassive *= (d.amount / 100f); // ★ CHG
            else                _atkAddPassive += d.amount;          // ★ CHG
            RecalcAttack();                                          // ★ CHG
            break;

        case StatType.CurrentMoney:
            // 돈은 기존처럼 즉시 적용(원하면 누적/재계산 모델로 확장 가능)
            if (d.isMultiplier)
                currentMoney = Mathf.RoundToInt(currentMoney * (d.amount / 100f)); // ★ CHG: 정밀도
            else
                currentMoney += Mathf.RoundToInt(d.amount);
            statDisplay?.SetCurrentMoney(currentMoney);
            break;

        case StatType.CurrentMoveSpeed:
            if (d.isMultiplier) _spdMulPassive *= (d.amount / 100f); // ★ CHG
            else                _spdAddPassive += d.amount;          // ★ CHG
            RecalcMoveSpeed();                                      // ★ CHG
            break;

        case StatType.CurrentDashCoolDown:
            if (d.isMultiplier) _dashMulPassive *= (d.amount / 100f); // ★ CHG
            else                _dashAddPassive += d.amount;           // ★ CHG
            RecalcDashCooldown();                                      // ★ CHG
            break;
    }
}

    // 해제(원복) – 간단화를 위해 반대로 적용
    public void RemoveStatDelta(StatDelta d)
{
    switch (d.stat)
    {
        case StatType.MaxHealth:
            if (d.isMultiplier)
            {
                float factor = (d.amount / 100f);
                if (factor != 0f) _maxMulPassive /= factor;     // ★ CHG
            }
            else
            {
                _maxAddPassive -= d.amount;                     // ★ CHG
            }
            RecalcMaxHealth();                                   // ★ CHG
            break;

        case StatType.CurrentHealth:
            // ★ CHG 없음: 특수 규칙 유지.
            // (OnPickup 같은 즉시효과의 되돌림을 원한다면 별도 설계 필요)
            {
                int initial2 = currentHealth;
                if (d.isMultiplier)
                {
                    // 네 기존 코드 로직을 그대로 유지하려면 역연산이 애매함.
                    // 필요시 '적용 당시 추가된 양'을 저장해 두었다가 되돌리는 방식 권장.
                    float factor = (d.amount / 100f);
                    currentHealth = Mathf.RoundToInt(currentHealth - (maxHealth * factor));
                }
                else
                    currentHealth = Mathf.Max(1, currentHealth - Mathf.RoundToInt(d.amount));
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                currentHealth = Mathf.Max(1, currentHealth);
                statDisplay?.SetCurrentHealth(currentHealth);
            }
            break;

        case StatType.CurrentAttackDamage:
            if (d.isMultiplier)
            {
                float factor = (d.amount / 100f);
                if (factor != 0f) _atkMulPassive /= factor;     // ★ CHG
            }
            else
            {
                _atkAddPassive -= d.amount;                     // ★ CHG
            }
            RecalcAttack();                                     // ★ CHG
            break;

        case StatType.CurrentMoney:
            if (d.isMultiplier)
            {
                float factor = (d.amount / 100f);
                if (factor != 0f) currentMoney = Mathf.RoundToInt(currentMoney / factor); // ★ CHG
            }
            else
            {
                currentMoney -= Mathf.RoundToInt(d.amount);
            }
            statDisplay?.SetCurrentMoney(currentMoney);
            break;

        case StatType.CurrentMoveSpeed:
            if (d.isMultiplier)
            {
                float factor = (d.amount / 100f);
                if (factor != 0f) _spdMulPassive /= factor;     // ★ CHG
            }
            else
            {
                _spdAddPassive -= d.amount;                     // ★ CHG
            }
            RecalcMoveSpeed();                                  // ★ CHG
            break;

        case StatType.CurrentDashCoolDown:
            if (d.isMultiplier)
            {
                float factor = (d.amount / 100f);
                if (factor != 0f) _dashMulPassive /= factor;    // ★ CHG
            }
            else
            {
                _dashAddPassive -= d.amount;                    // ★ CHG
            }
            RecalcDashCooldown();                               // ★ CHG
            break;
    }
}
    
    public void ApplyDeltas(IEnumerable<StatDelta> list)
    {
        foreach (var d in list)
        {
            ApplyStatDelta(d);
            _appliedDeltas.Add(d);
        }
    }

    public void RemoveDeltas(IEnumerable<StatDelta> list)
    {
        foreach (var d in list)
        {
            RemoveStatDelta(d);
            _appliedDeltas.Remove(d);
        }
    }
    
    void RecalcMaxHealth()
    {
        // 최종 Max = round( (base + addSum) * mulProduct )
        int newMax = Mathf.Max(1, Mathf.RoundToInt( (_baseMaxHealth + _maxAddPassive) * _maxMulPassive ));
        maxHealth = newMax;

        // currentHealth는 자동 회복/손실 없이 안전하게 클램프만
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        statDisplay?.SetMaxHealth(maxHealth);
        statDisplay?.SetCurrentHealth(currentHealth);
    }

    void RecalcAttack()
    {
        currentAttackDamage = Mathf.Max(0,
            Mathf.RoundToInt( (_baseStartAttack + _atkAddPassive) * _atkMulPassive ));
        statDisplay?.SetAttackDamage(currentAttackDamage);
    }

    void RecalcMoveSpeed()
    {
        currentMoveSpeed = Mathf.Max(0,
            Mathf.RoundToInt( (_baseStartMoveSpeed + _spdAddPassive) * _spdMulPassive ));
        controller.moveSpeed = currentMoveSpeed;
        statDisplay?.SetCurrentMoveSpeed(currentMoveSpeed);
    }

    void RecalcDashCooldown()
    {
        currentDashCoolDown = Mathf.Max(0,
            Mathf.RoundToInt( (_baseStartDashCoolDown + _dashAddPassive) * _dashMulPassive ));
        controller.dashCooldown = currentDashCoolDown;
    }
    
    private IEnumerator HitFlashCo()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;

        // 원래 색 저장
        var origs = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (!spriteRenderers[i]) continue;
            origs[i] = spriteRenderers[i].color;

            // 알파는 유지하면서 붉은 색 틴트
            var o = origs[i];
            spriteRenderers[i].color = new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, o.a);
        }

        yield return new WaitForSeconds(hitFlashDuration);

        // 원상복귀
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (!spriteRenderers[i]) continue;
            spriteRenderers[i].color = origs[i];
        }
        _hitFlashCo = null;
    }

    private IEnumerator InvincibleBlinkCo()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;

        // 무적 동안 빠르게 깜빡깜빡 (enable 토글)
        bool visible = true;
        while (isInvincible)
        {
            visible = !visible;
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i]) spriteRenderers[i].enabled = visible;

            yield return new WaitForSeconds(invincibleBlinkInterval);
        }

        // 무적 종료: 반드시 보이게 원복
        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i]) spriteRenderers[i].enabled = true;

        _blinkCo = null;
    }
    
}
    


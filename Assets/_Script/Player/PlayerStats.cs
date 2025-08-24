using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    public StatDisplay statDisplay;
    
    [NonSerialized] public int startAttackDamage = 1;
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
        
        int totalDamage = Mathf.RoundToInt(amount * (GameManager.Instance.LevelCoefficient));
        currentHealth -= totalDamage;
        
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
        int totalDamage = Mathf.RoundToInt(amount * (GameManager.Instance.LevelCoefficient));
        currentHealth -= totalDamage;
        if (currentHealth < 0)
            currentHealth = 0;
        
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

    public void AddMoney(int amount)
    {
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
                // 퍼센트 표기: 120 => 1.20배
                _maxMulPassive *= (d.amount / 100f);        // ★ CHG
            }
            else
            {
                _maxAddPassive += d.amount;                 // ★ CHG
            }
            RecalcMaxHealth();                              // ★ CHG
            break;

        case StatType.CurrentHealth:
            // ★ CHG 없음: 너가 만든 특수 규칙 유지
            // multiplier면 (maxHealth * n%)만큼 현재 체력에 '추가'
            // additive면 정수 가감
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
    
}
    


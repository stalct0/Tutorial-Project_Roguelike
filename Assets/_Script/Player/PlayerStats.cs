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
        
        currentHealth -= amount;
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
        
        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;
        if (statDisplay != null)
            statDisplay.SetCurrentHealth(currentHealth);
        
        // 이 시점에서 넉백 먼저 적용
        
        
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
                int initial = currentHealth;
                if (d.isMultiplier)
                {
                    maxHealth = Mathf.RoundToInt(maxHealth * (1f + d.amount));
                    currentHealth = Mathf.RoundToInt(currentHealth * (1f + d.amount));
                }
                else
                {
                    maxHealth = Mathf.Max(1, maxHealth + Mathf.RoundToInt(d.amount));
                    currentHealth = Mathf.Max(1, currentHealth + Mathf.RoundToInt(d.amount));
                }
                currentHealth = Mathf.Max(initial, currentHealth);
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                statDisplay?.SetMaxHealth(maxHealth);
                statDisplay?.SetCurrentHealth(currentHealth);
                break;
            
            case StatType.CurrentHealth:
                int initial2 = currentHealth;
                if (d.isMultiplier) currentHealth = Mathf.RoundToInt(currentHealth + (maxHealth*d.amount));
                else currentHealth = Mathf.Max(1, currentHealth + Mathf.RoundToInt(d.amount));
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                currentHealth = Mathf.Max(initial2, currentHealth);
                statDisplay?.SetCurrentHealth(currentHealth);
                break;

            case StatType.CurrentAttackDamage:
                if (d.isMultiplier) currentAttackDamage = Mathf.RoundToInt(currentAttackDamage * (1f + d.amount));
                else currentAttackDamage += Mathf.RoundToInt(d.amount);
                statDisplay?.SetAttackDamage(currentAttackDamage);
                break;
            
            case StatType.CurrentMoney:
                if (d.isMultiplier) currentMoney = Mathf.RoundToInt(currentMoney * (1f + d.amount));
                else currentMoney += Mathf.RoundToInt(d.amount);
                statDisplay?.SetCurrentMoney(currentMoney);
                break;

            case StatType.CurrentMoveSpeed:
                if (d.isMultiplier) currentMoveSpeed = Mathf.RoundToInt(currentMoveSpeed * (1f + d.amount));
                else currentMoveSpeed += Mathf.RoundToInt(d.amount);
                controller.moveSpeed = currentMoveSpeed;
                statDisplay?.SetCurrentMoveSpeed(currentMoveSpeed);
                break;
        }
    }

    // 해제(원복) – 간단화를 위해 반대로 적용
    public void RemoveStatDelta(StatDelta d)
    {
        StatDelta inv = d;
        inv.amount = d.isMultiplier ? (-d.amount / (1f + d.amount)) : (-d.amount);
        ApplyStatDelta(inv);
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
}
    


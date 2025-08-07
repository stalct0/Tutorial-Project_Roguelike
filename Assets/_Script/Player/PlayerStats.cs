using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    public StatDisplay statDisplay;
    
    public int maxHealth = 100;
    public int startAD = 1;
    public int startMoney = 0;
    public int currentHealth { get; private set; }
    public int attackDamage { get; private set; }
    public int abilityPower { get; private set; }
    
    public bool isInvincible = false;
    private float invincibleTimer = 0f;
    
    public int Money { get; private set; }
    
    public UnityEvent onDie;
    public PlayerController controller; 

    void Awake()
    {
        statDisplay = GameObject.Find("Canvas").GetComponent<StatDisplay>();
        controller = GetComponent<PlayerController>();

        currentHealth = maxHealth;
        attackDamage = startAD;
        Money = startMoney;
        
        if (statDisplay != null)
        {
            statDisplay.SetMaxHealth(maxHealth);
            statDisplay.SetStat(attackDamage);
            statDisplay.SetMoney(Money);
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
            statDisplay.SetHealth(currentHealth);
        
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
            statDisplay.SetHealth(currentHealth);
        
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
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        if (statDisplay != null)
            statDisplay.SetHealth(currentHealth);
    }

    public void ChangeStats()
    {
        statDisplay.SetStat(attackDamage);
    }

    public void ChangeMoney(int money)
    {
        statDisplay.SetMoney(money);
    }

    private void Die()
    {
        if (onDie != null)
            onDie.Invoke();
    }

    public void ResetStats()
    {
        currentHealth = maxHealth;
    }
    
    
}

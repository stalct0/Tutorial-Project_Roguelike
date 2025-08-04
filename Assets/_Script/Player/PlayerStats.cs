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
    
    public int Money { get; private set; }
    
    public UnityEvent onDie;

    void Awake()
    {
        statDisplay = GameObject.Find("Canvas").GetComponent<StatDisplay>();

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
    
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;
        if (statDisplay != null)
            statDisplay.SetHealth(currentHealth);

        if (currentHealth <= 0)
            Die();
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

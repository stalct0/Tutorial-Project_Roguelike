using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth { get; private set; }
    public int attackDamage { get; private set; }
    public int abilityPower { get; private set; }
    
    public UnityEvent onDie;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
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

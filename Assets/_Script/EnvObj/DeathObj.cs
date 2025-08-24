using UnityEngine;

public class DeathObj : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.InstantDeath();
            }
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyCombat combat = other.GetComponent<EnemyCombat>();
            if (combat)
            {
                combat.InstaDie();
            }
        }
    }
}

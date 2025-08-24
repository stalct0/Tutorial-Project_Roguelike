using UnityEngine;

public class DeathObj : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // PlayerStats 컴포넌트 찾아서 체력을 0으로
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.InstantDeath();
            }
        }
    }
}

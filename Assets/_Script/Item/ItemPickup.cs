using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemDefinition item;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var inv = other.GetComponent<PlayerInventory>();
        if (inv == null) return;

        if (inv.TryAdd(item))
        {
            Destroy(gameObject); // 주웠으면 제거
        }
        else
        {
            // 가득 차서 못 줍는 경우: UI로 알림 등
        }
    }
}
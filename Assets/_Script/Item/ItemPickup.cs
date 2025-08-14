using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemDefinition item;

    // 플레이어가 집을 때 호출
    public bool TryPickup(PlayerInventory inv)
    {
        if (inv == null || item == null) return false;
        bool ok = inv.TryAdd(item);
        if (ok) Destroy(gameObject);
        return ok;
    }
}
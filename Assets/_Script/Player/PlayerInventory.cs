using System.Collections;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int slotCount = 3;
    public ItemDefinition[] slots;

    private PlayerStats stats;

    void Awake()
    {
        slots = new ItemDefinition[slotCount];
        stats = GetComponent<PlayerStats>();
    }

    public bool TryAdd(ItemDefinition item)
    {
        // 즉시발동형: 소지하지 않고 바로 적용 후 끝
        if (item.kind == ItemKind.OnPickup)
        {
            ApplyItem(item);
            return true;
        }

        // 빈칸에 넣기
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                // 패시브는 들고 있는 동안 효과 적용
                if (item.kind == ItemKind.Passive)
                    ApplyItem(item);
                // UI 갱신 훅 (나중에 InventoryUI에서 받아서 그려주기)
                InventoryChanged?.Invoke();
                return true;
            }
        }
        return false; // 가득 참
    }

    public void UseSlot(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return;
        var item = slots[idx];
        if (item == null) return;

        if (item.kind == ItemKind.Consumable)
        {
            ApplyItem(item);
            if (item.consumeOnUse)
            {
                slots[idx] = null;
                InventoryChanged?.Invoke();
            }
        }
        // Passive는 사용 개념 없음(들고 있으면 적용 상태)
    }

    public void RemoveSlot(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return;
        var item = slots[idx];
        if (item == null) return;

        if (item.kind == ItemKind.Passive)
            RemoveItem(item);

        slots[idx] = null;
        InventoryChanged?.Invoke();
    }

    // 효과 적용/해제
    void ApplyItem(ItemDefinition item)
    {
        if (item.hasDuration && item.durationSec > 0f)
        {
            StartCoroutine(stats.ApplyTimedDeltas(item.deltas, item.durationSec));
        }
        else
        {
            stats.ApplyDeltas(item.deltas);
        }
    }
    void RemoveItem(ItemDefinition item)
    {
        if (!item.hasDuration) // 지속형만 원복
            stats.RemoveDeltas(item.deltas);
        // duration이 있는 건 코루틴이 알아서 원복함
    }

    // UI가 구독할 이벤트 (간단한 델리게이트)
    public System.Action InventoryChanged;
}

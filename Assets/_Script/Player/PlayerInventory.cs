using System.Collections;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int slotCount = 3;
    public ItemDefinition[] slots;

    private PlayerStats stats;
    
    [ReadOnly] public ItemDefinition lastConsumedItem; // 최근 먹은 OnPickup 아이템을 보관
    public GameObject pickupPrefab;   // 드롭할 때 생성할 ItemPickup 프리팹
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
            lastConsumedItem = item;      // 최근 먹은 즉발 아이템 기억
            ApplyItem(item);              // 효과 적용
            InventoryChanged?.Invoke();   // UI 갱신 트리거
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
    
    public bool TryDrop(int idx, Vector3 worldPos, Vector2 throwImpulse)
    {
        if (idx < 0 || idx >= slots.Length) return false;

        var item = slots[idx];
        if (item == null) return false;

        // 이번 요구사항: "Passive만" 드롭 가능
        if (item.kind != ItemKind.Passive)
            return false;

        // 패시브 효과 해제 + 인벤토리에서 제거
        RemoveItem(item);         // 기존 함수: 패시브면 원복
        slots[idx] = null;
        InventoryChanged?.Invoke();

        // 월드에 ItemPickup 생성
        if (pickupPrefab != null)
        {
            var go = GameObject.Instantiate(pickupPrefab, worldPos, Quaternion.identity);

            // 아이템 지정
            var pickup = go.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.item = item;
            }

            // 살짝 튀어나오게
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.AddForce(throwImpulse, ForceMode2D.Impulse);
            }
        }

        return true;
    }
    public bool HasEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return true;
        return false;
    }
}

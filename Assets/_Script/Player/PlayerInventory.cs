using System.Collections;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int slotCount = 3;
    public ItemDefinition[] slots;

    private PlayerStats stats;
    
    [ReadOnly] public ItemDefinition lastConsumedItem; // 최근 먹은 OnPickup 아이템을 보관
    public GameObject pickupPrefab;   // 드롭할 때 생성할 ItemPickup 프리팹
    
    [ReadOnly] public int[] slotRolledValues;      // ★ ADD: 슬롯별 굴림 값
    [ReadOnly] public int  lastConsumedRolledValue; // ★ ADD: 최근 소모 굴림 값

    
    
    void Awake()
    {
        slots = new ItemDefinition[slotCount];
        stats = GetComponent<PlayerStats>();
        slotRolledValues = new int[slotCount];
    }

    public bool TryAdd(ItemDefinition item, int rolledValue)
    {
        // 즉시발동형: 소지하지 않고 바로 적용 후 끝
        if (item.kind == ItemKind.OnPickup)
        {
            lastConsumedItem = item;      // 최근 먹은 즉발 아이템 기억
            lastConsumedRolledValue = rolledValue;
            ApplyItem(item, rolledValue);              // 효과 적용
            InventoryChanged?.Invoke();   // UI 갱신 트리거
            return true;
        }

        // 빈칸에 넣기
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                slotRolledValues[i] = rolledValue;
                // 패시브는 들고 있는 동안 효과 적용
                if (item.kind == ItemKind.Passive)
                    ApplyItem(item, rolledValue);
                // UI 갱신 훅 (나중에 InventoryUI에서 받아서 그려주기)
                InventoryChanged?.Invoke();
                return true;
            }
        }
        return false; // 가득 참
    }
    
    public bool TryAdd(ItemDefinition item)                // ★ CHG
    {
        if (item == null) return false;
        int rolled = Random.Range(item.minRoll, item.maxRoll + 1);  // ★ ADD
        return TryAdd(item, rolled);                                 // ★ CHG
    }
    
    public void RemoveSlot(int idx)
    {
        if (idx < 0 || idx >= slots.Length) return;
        var item = slots[idx];
        if (item == null) return;

        if (item.kind == ItemKind.Passive)
            RemoveItem(item, slotRolledValues[idx]);

        slots[idx] = null;
        slotRolledValues[idx] = 0;
        InventoryChanged?.Invoke();
    }

    // 효과 적용/해제
    void ApplyItem(ItemDefinition item, int rolledValue)
    {

        stats.ApplyDeltas(BuildScaledDeltas(item, rolledValue));
    }
    
    void RemoveItem(ItemDefinition item, int rolledValue)
    {
        stats.RemoveDeltas(BuildScaledDeltas(item, rolledValue));
    }

    // 필요 시 다른 코드 호환용(구시그니처) — 내부적으로 1배 스케일
    void ApplyItem(ItemDefinition item)  { ApplyItem(item, 1); }     
    void RemoveItem(ItemDefinition item) { RemoveItem(item, 1); }     
    
    System.Collections.Generic.List<StatDelta> BuildScaledDeltas(ItemDefinition item, int rolledValue) // ★ ADD
    {
        var list = new System.Collections.Generic.List<StatDelta>(item.deltas.Count);
        foreach (var d in item.deltas)
        {
            var s = d;
            s.amount = rolledValue; // 더하기/곱하기 여부는 isMultiplier가 말해줌(네 쪽 로직 유지)
            list.Add(s);
        }
        return list;
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

        int rolled = slotRolledValues[idx];        
        
        // 패시브 효과 해제 + 인벤토리에서 제거
        RemoveItem(item, rolled);         // 기존 함수: 패시브면 원복
        slots[idx] = null;
        InventoryChanged?.Invoke();
        slotRolledValues[idx] = 0;    
        
        // 월드에 ItemPickup 생성
        if (pickupPrefab != null)
        {
            var go = GameObject.Instantiate(pickupPrefab, worldPos, Quaternion.identity);

            // 아이템 지정
            var pickup = go.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.Set(item, rolled);
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

using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemDefinition item;
    
    [Header("Rolled Value")]           // ★ ADD
    public int rolledValue;            // 이 픽업의 굴려진 수치
    public bool hasRolled;             // 이미 굴렸는가?
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;   // 비워두면 자동 할당
    [SerializeField] private string sortingLayerName = "Item";
    [SerializeField] private int sortingOrder = 0;
    
    
    
    // 플레이어가 집을 때 호출
    public bool TryPickup(PlayerInventory inv)
    {
        if (inv == null || item == null) return false;
        int rolled = EnsureRolled();
        bool ok = inv.TryAdd(item, rolled);
        if (ok) Destroy(gameObject);
        return ok;
    }
    
    void Reset()
    {
        // 에디터에서 컴포넌트 붙일 때 자동 연결
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ApplyVisual();
    }

    void Start()
    {
        ApplyVisual();
    }
    
    
    
    // 에디터에서 item 바꿀 때도 즉시 반영되게
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ApplyVisual();
    }
#endif
    
    public void SetItem(ItemDefinition def)
    {
        item = def;
        hasRolled = false;                      // 아직 미확정
        ApplyVisual();
    }
    
    // ▶ 상자/상점 등에서 "미리 굴려서" 픽업을 만들 때 사용
    public void Set(ItemDefinition def, int rolled) // ★ ADD
    {
        item = def;
        rolledValue = rolled;
        hasRolled = true;
        ApplyVisual();
    }

    // ▶ 필요하면 이 순간에 한 번만 굴린다(인벤/상자 어디서 와도 일관)
    public int EnsureRolled()                   // ★ ADD
    {
        if (!item) return 0;
        if (!hasRolled)
        {
            rolledValue = Random.Range(item.minRoll, item.maxRoll + 1);
            hasRolled = true;
        }
        return rolledValue;
    }


    private void ApplyVisual()
    {
        if (!spriteRenderer) return;

        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;

        if (item != null && item.icon != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = item.icon;
        }
        else
        {
            // 데이터가 없거나 아이콘이 없으면 숨김
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
        }
    }
}
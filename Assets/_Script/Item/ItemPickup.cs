using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemDefinition item;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;   // 비워두면 자동 할당
    [SerializeField] private string sortingLayerName = "Item";
    [SerializeField] private int sortingOrder = 0;
    
    
    
    // 플레이어가 집을 때 호출
    public bool TryPickup(PlayerInventory inv)
    {
        if (inv == null || item == null) return false;
        bool ok = inv.TryAdd(item);
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
        ApplyVisual();
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
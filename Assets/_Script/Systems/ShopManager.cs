using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;

[System.Serializable]
public class ShopItemData
{
    public ItemDefinition def;
    public int price;
}
public class ShopManager : MonoBehaviour
{
    [Header("Loot Config")]
    public LootTable lootTable;                // 상점에 뿌릴 아이템 테이블
    public bool includeOnPickup = true;        // OnPickup 아이템도 판매 목록에 포함할지
    public int currentStageForFilter = 0;      // 필요하면 스테이지 기반 필터링

    [Header("Price Config")]
    public Vector2Int priceRange = new Vector2Int(10, 50); // 예: 10~50 사이 랜덤
    
    [Header("UI - 3개 슬롯")]
    public ShopItemView[] slots = new ShopItemView[3];

    [Header("Highlight")]
    public Image[] highlightFrames = new Image[3]; // 선택 프레임(없으면 비워둬도 됨)
    public Color highlightOn = Color.white;
    public Color highlightOff = new Color(1,1,1,0.2f);
    
    [Header("Input")]
    [NonSerialized] public KeyCode leftKey = KeyCode.LeftArrow;
    [NonSerialized] public KeyCode rightKey = KeyCode.RightArrow;
    [NonSerialized] public KeyCode buyKey = KeyCode.Return;

    // 내부 상태
    private ShopItemData[] items = new ShopItemData[3];   // ✅ 아이템+가격 묶음
    private int selectedIndex = 0;

    // 캐시
    private PlayerInventory inv;
    private PlayerStats pstats;

    void OnEnable()
    {
        // GameManager에서 플레이어 캐시 가져오기
        inv = GameManager.Instance ? GameManager.Instance.PInventory : null;
        pstats = GameManager.Instance ? GameManager.Instance.PStats : null;

        // 판매 목록 구성
        RollItems();

        // UI 갱신
        RefreshAll();
        UpdateHighlight();
    }
    void Awake()
    {
        Tilemap ladderTilemap = GameObject.Find("LadderTilemap").GetComponent<Tilemap>();
        GameManager.Instance.SpawnOrMovePlayer(Vector3.zero, ladderTilemap);
    }
    
    void Update()
    {
        //넘어가기
        if (Input.GetKeyDown(KeyCode.Q))
        {
            GameManager.Instance.NextStage();
            GameManager.Instance.LoadScene("Level");
        }
        
        // 좌우 선택
        if (Input.GetKeyDown(leftKey))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(rightKey))
        {
            MoveSelection(+1);
        }

        if (Input.GetKeyDown(buyKey))
        {
            TryBuySelected();
        }
    }

    void MoveSelection(int delta)
    {
        int old = selectedIndex;
        selectedIndex = Mathf.Clamp(selectedIndex + delta, 0, slots.Length - 1);
        if (selectedIndex != old) UpdateHighlight();
    }

    void UpdateHighlight()
    {
        for (int i = 0; i < highlightFrames.Length; i++)
        {
            if (highlightFrames[i] == null) continue;
            highlightFrames[i].color = (i == selectedIndex) ? highlightOn : highlightOff;
        }
    }

    void RollItems()
    {
        if (lootTable == null) return;

        // 중복 허용/비허용 선택: 보통 상점은 중복 없이 주는 편이라 중복 회피 로직 추가
        HashSet<ItemDefinition> used = new HashSet<ItemDefinition>();
        for (int i = 0; i < items.Length; i++)
        {
            ItemDefinition pick = null;
            int guard = 50;
            while (guard-- > 0)
            {
                pick = lootTable.Roll(currentStageForFilter, includeOnPickup);
                if (pick == null) break;
                if (!used.Contains(pick)) break;
            }
            if (pick != null) used.Add(pick);
            
            // ✅ 가격은 여기서 랜덤으로 정해줌
            int randPrice = Random.Range(priceRange.x, priceRange.y + 1);
            
            items[i] = new ShopItemData { def = pick, price = randPrice };
        }
    }

    void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var data = (i < items.Length) ? items[i] : null;
            slots[i].Set(data);
        }
    }
    int GetPlayerMoney()
    {
        return (pstats != null) ? pstats.currentMoney : 0;
    }

    bool TrySpend(int price)
    {
        if (pstats == null) return false;
        return pstats.TrySpendMoney(price); // ← PlayerStats 헬퍼 호출
    }

    void TryBuySelected()
    {
        if (inv == null || items == null) return;
        if (selectedIndex < 0 || selectedIndex >= items.Length) return;

        var data = items[selectedIndex];
        if (data == null || data.def == null) return;

        int price = data.price;

        // 1) 돈 체크
        if (GetPlayerMoney() < price)
        {
            return;
        }

        // 2) 인벤 꽉 차면 불가
        if (!inv.HasEmptySlot())
        {

            return;
        }

        // 3) 결제
        if (!TrySpend(price))
        {

            return;
        }

        // 4) 아이템 지급
        bool ok = inv.TryAdd(data.def);
        if (!ok) return;

        // 5) 슬롯 비우기 + UI 갱신
        items[selectedIndex] = null;
        slots[selectedIndex].Set(null);
    }
}

[System.Serializable]
public class ShopItemView
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text priceText;

    public void Set(ShopItemData data)
    {
        if (data == null || data.def == null)
        {
            if (icon) { icon.enabled = false; icon.sprite = null; }
            if (nameText) nameText.text = "";
            if (descText) descText.text = "";
            if (priceText) priceText.text = "";
            return;
        }

        if (icon) { icon.enabled = (data.def.icon != null); icon.sprite = data.def.icon; }
        if (nameText) nameText.text = data.def.displayName;
        if (descText) descText.text = data.def.description;
        if (priceText) priceText.text = $"$ {data.price}";
    }
}

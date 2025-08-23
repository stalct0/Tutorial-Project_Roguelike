using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryScreen : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject panelRoot;   // 인벤토리 전체 패널(켜기/끄기)
    [SerializeField] private TextMeshProUGUI title;  // "Inventory" 같은 제목(옵션)

    [System.Serializable]
    public class SlotView
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
    }
    
    [Header("소모품")]
    [SerializeField] private SlotView[] cslots = new SlotView[1];
    
    [Header("3개 슬롯 뷰")]
    [SerializeField] private SlotView[] slots = new SlotView[3];


    private KeyCode toggleKey = KeyCode.E;     // 열기/닫기
    private KeyCode dropKey1 = KeyCode.Alpha1; // 1
    private KeyCode dropKey2 = KeyCode.Alpha2; // 2
    private KeyCode dropKey3 = KeyCode.Alpha3; // 3

    private PlayerInventory inv;
    private bool isOpen = false;
    private float prevTimeScale = 1f;

    void OnEnable()
    {
        GameManager.Instance.OnInventoryReady += Bind;

        // 이미 준비된 상태면 즉시 바인딩
        if (GameManager.Instance?.PInventory != null)
            Bind(GameManager.Instance.PInventory);

        // 시작은 닫힌 상태 권장
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnInventoryReady -= Bind;

        if (inv != null)
            inv.InventoryChanged -= Refresh;
    }

    void Update()
    {
        
        // E로 열고/닫기
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen) Close();
            else
            {
                if (GameManager.Instance.CurrentState == GameManager.GameState.Playing || GameManager.Instance.CurrentState == GameManager.GameState.Shop)
                {
                    Open();
                }
            }
        }
        
        if (Input.GetKeyDown(dropKey1)) DropSlot(0);
        if (Input.GetKeyDown(dropKey2)) DropSlot(1);
        if (Input.GetKeyDown(dropKey3)) DropSlot(2);
        
        if (!isOpen || inv == null) return;

        // 열린 동안 1/2/3으로 버리기
        
    }
    
    void Bind(PlayerInventory inventory)
    {
        if (inv != null) inv.InventoryChanged -= Refresh;
        inv = inventory;
        if (inv != null) inv.InventoryChanged += Refresh;
        Refresh();
    }

    
    void Refresh()
    {
        if (inv == null || slots == null) return;
        // 3칸 인벤토리 렌더
        for (int i = 0; i < slots.Length; i++)
        {
            var slotView = slots[i];
            var item = (i < inv.slots.Length) ? inv.slots[i] : null;

            if (item != null)
            {
                if (slotView.icon != null)
                {
                    slotView.icon.enabled = true;
                    slotView.icon.sprite = item.icon;
                }
                if (slotView.nameText) slotView.nameText.text = item.displayName;
                if (slotView.descText)
                {
                    int rolled = (i < inv.slotRolledValues.Length) ? inv.slotRolledValues[i] : 0; // ★ ADD
                    string fmt = string.IsNullOrEmpty(item.uiEffectFormat) ? "+{0}" : item.uiEffectFormat; // ★ ADD
                    string eff = string.Format(fmt, rolled);                                               // ★ ADD
                    slotView.descText.text = $"{item.description}\n{eff}";                                 // ★ CHG
                }
            }
            else
            {
                if (slotView.icon != null)
                {
                    slotView.icon.enabled = false;
                    slotView.icon.sprite = null;
                }
                if (slotView.nameText != null) slotView.nameText.text = "";
                if (slotView.descText != null) slotView.descText.text = "";
            }
        }
        
        // 최근 먹은 OnPickup(= lastConsumedItem) 표시
        if (cslots != null && cslots.Length > 0)
        {
            var view = cslots[0];
            var last = inv.lastConsumedItem;  

            if (last != null)
            {
                if (view.icon != null) { view.icon.enabled = true; view.icon.sprite = last.icon; }
                if (view.nameText != null) view.nameText.text = last.displayName;
                if (view.descText)
                {
                    string fmt = string.IsNullOrEmpty(last.uiEffectFormat) ? "+{0}" : last.uiEffectFormat; // ★ ADD
                    string eff = string.Format(fmt, inv.lastConsumedRolledValue);                           // ★ ADD
                    view.descText.text = $"{last.description}\n{eff}";                                     // ★ CHG
                }
            }
            else
            {
                // last가 없을 때는 전부 비우기
                if (view.icon) { view.icon.enabled = false; view.icon.sprite = null; }
                if (view.nameText) view.nameText.text = "";
                if (view.descText) view.descText.text = "";
            }
        }
    }
    
    void Open()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Inventory);
        if (panelRoot == null) return;
        isOpen = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;                          // 시간 정지
        panelRoot.SetActive(true);                    // UI 표시
        Refresh();
    }

    void Close()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Playing);
        if (panelRoot == null) return;
        isOpen = false;
        Time.timeScale = prevTimeScale;               // 시간 복구
        panelRoot.SetActive(false);                   // UI 숨김
    }

    // ────────────────────────────── 버리기 ──────────────────────────────

    void DropSlot(int index)
    {
        if (inv == null) return;

        // 플레이어 위치/방향을 기준으로 드롭 지점 및 임펄스 계산
        var pstats = GameManager.Instance?.PStats;
        if (pstats == null) return;

        // 플레이어 약간 위쪽에서 툭 떨어뜨리듯 생성
        Vector3 dropOrigin = pstats.transform.position + Vector3.up * 0.2f;

        // 바라보는 방향(로컬 스케일 x 부호)으로 약간 튀게
        float dirX = Mathf.Sign(pstats.transform.localScale.x);
        Vector2 throwImpulse = new Vector2(1.5f * dirX, 2.5f);

        // 패시브만 드롭 가능. 성공 시 UI 갱신
        bool ok = inv.TryDrop(index, dropOrigin, throwImpulse);
        if (ok) Refresh();
    }
}
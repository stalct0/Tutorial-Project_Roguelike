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

    [Header("3개 슬롯 뷰")]
    [SerializeField] private SlotView[] slots = new SlotView[3];

    [Header("입력 키")]
    [SerializeField] private KeyCode toggleKey = KeyCode.E;     // 열기/닫기
    [SerializeField] private KeyCode dropKey1 = KeyCode.Alpha1; // 1
    [SerializeField] private KeyCode dropKey2 = KeyCode.Alpha2; // 2
    [SerializeField] private KeyCode dropKey3 = KeyCode.Alpha3; // 3

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
        if (inv == null)
        {
            Debug.Log("inv is null");
        }
        
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

        if (!isOpen || inv == null) return;

        // 열린 동안 1/2/3으로 버리기
        if (Input.GetKeyDown(dropKey1)) DropSlot(0);
        if (Input.GetKeyDown(dropKey2)) DropSlot(1);
        if (Input.GetKeyDown(dropKey3)) DropSlot(2);
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
                if (slotView.nameText != null) slotView.nameText.text = item.displayName;
                if (slotView.descText != null) slotView.descText.text = item.description;
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
        inv.RemoveSlot(index); // Passive면 효과 원복, Consumable은 그냥 제거
        Refresh();
    }
}
// InventoryUI.cs (하드닝 버전)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    [Header("Slots (UI Images)")]
    [SerializeField] private Image[] slotImages;   // 인스펙터에 3칸 연결

    private PlayerInventory inv;
    private Coroutine bindRoutine;

    void OnEnable()
    {
        // 1) 즉시 바인드 시도 (이벤트를 놓쳐도 복구)
        TryImmediateBind();

        // 2) 늦게 생성되는 케이스 대비: 이벤트 구독 + GM 대기
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnInventoryReady += HandleInventoryReady; // 인벤토리 준비 신호 듣기
        }
        else
        {
            // DontDestroyOnLoad 순서/씬 로딩 지연 대비
            bindRoutine = StartCoroutine(WaitForGameManagerThenBind());
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnInventoryReady -= HandleInventoryReady;

        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        if (inv != null) inv.InventoryChanged -= Refresh;
    }

    // --- 바인딩 흐름 ---
    void HandleInventoryReady(PlayerInventory pinv) => Bind(pinv);

    void TryImmediateBind()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.PInventory != null)
        {
            Bind(gm.PInventory);
        }
        else
        {
            // 아직 못 찾았으면 일단 UI를 깨끗이
            SafeClear();
        }
    }

    IEnumerator WaitForGameManagerThenBind()
    {
        // 실시간(일시정지 무시)으로 3초까지 대기
        float start = Time.realtimeSinceStartup;
        while (GameManager.Instance == null && Time.realtimeSinceStartup - start < 3f)
            yield return null;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnInventoryReady += HandleInventoryReady;

            // GM이 이미 플레이어를 만들어둔 상태면 즉시 바인드
            if (gm.PInventory != null)
                Bind(gm.PInventory);
        }
        bindRoutine = null;
    }

    void Bind(PlayerInventory target)
    {
        if (target == null) return;

        if (inv != null) inv.InventoryChanged -= Refresh; // 중복 구독 해제
        inv = target;
        inv.InventoryChanged += Refresh;
        Refresh();
    }

    // --- UI 갱신 ---
    void Refresh()
    {
        if (slotImages == null) return;

        int uiCount  = slotImages.Length;
        int invCount = (inv != null && inv.slots != null) ? inv.slots.Length : 0;
        int count    = Mathf.Min(uiCount, invCount);

        for (int i = 0; i < uiCount; i++)
        {
            var img = slotImages[i];
            if (!img) continue;

            if (i < count && inv.slots[i] != null && inv.slots[i].icon != null)
            {
                img.sprite  = inv.slots[i].icon;
                img.enabled = true;
            }
            else
            {
                img.sprite  = null;
                img.enabled = false;
            }
        }
    }

    void SafeClear()
    {
        if (slotImages == null) return;
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (!slotImages[i]) continue;
            slotImages[i].sprite  = null;
            slotImages[i].enabled = false;
        }
    }
}
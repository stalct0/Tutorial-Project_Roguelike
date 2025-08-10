// InventoryUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    public Image[] slotImages;  // 3칸 Image 연결
    private PlayerInventory inv;

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnInventoryReady += HandleInventoryReady;

        // 이미 플레이어가 준비되어 있다면 즉시 바인딩 시도
        TryBindImmediate();

        // 그래도 못 찾으면 잠깐 재시도(생성 타이밍 늦을 때)
        if (inv == null) StartCoroutine(TryBindUntilFound());
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnInventoryReady -= HandleInventoryReady;

        if (inv != null) inv.InventoryChanged -= Refresh;
    }

    void HandleInventoryReady(PlayerInventory pinv)
    {
        Bind(pinv);
    }

    void TryBindImmediate()
    {
        var pinv = GameManager.Instance?.PInventory;
        if (pinv != null) Bind(pinv);
    }

    IEnumerator TryBindUntilFound()
    {
        float t = 0f;
        while (inv == null && t < 2f)
        {
            TryBindImmediate();
            if (inv != null) yield break;
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
        if (inv == null) Debug.LogError("InventoryUI: PlayerInventory를 찾지 못했습니다.");
    }

    void Bind(PlayerInventory target)
    {
        if (target == null) return;

        if (inv != null) inv.InventoryChanged -= Refresh; // 중복 구독 방지
        inv = target;
        inv.InventoryChanged += Refresh;
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            var icon = (inv != null && inv.slots[i] != null) ? inv.slots[i].icon : null;
            slotImages[i].sprite = icon;
            slotImages[i].enabled = icon != null;
        }
    }
}
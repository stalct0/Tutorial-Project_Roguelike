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
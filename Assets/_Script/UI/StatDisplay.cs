using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatDisplay : MonoBehaviour
{
    public Slider slider;
    public TMP_Text statText;
    public TMP_Text stageText;
    public TMP_Text moneyText;
    public Image[] slotImages;
    
    private PlayerInventory inv;
    
    void Start()
    {
        SetStage($"{GameManager.Instance.GameStage}-{GameManager.Instance.GameLevel}");  // ✔️ 이게 맞는 함수
        inv = GameManager.Instance?.PC?.GetComponent<PlayerInventory>();
        if (inv != null)
        {
            inv.InventoryChanged += Refresh;
            Refresh();
        }
    }

    public void SetStat(int attackDamage)
    {
        statText.text = $"{attackDamage}";
    }
    public void SetStage(string stage)
    {
        stageText.text = $"{stage}";
    }

    public void SetMoney(int money)
    {
        moneyText.text = $"{money}";
    }
    
    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
    }

    public void SetHealth(int health)
    {
        slider.value = health;
    }
    
    void OnDestroy()
    {
        if (inv != null) inv.InventoryChanged -= Refresh;
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
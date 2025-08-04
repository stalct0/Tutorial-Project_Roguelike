using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatDisplay : MonoBehaviour
{
    public Slider slider;
    public TMP_Text statText;
    public TMP_Text stageText;
    public TMP_Text moneyText;
    
    void Start()
    {
        SetStage($"{GameManager.Instance.GameStage}-{GameManager.Instance.GameLevel}");  // ✔️ 이게 맞는 함수
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
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatDisplay : MonoBehaviour
{
    public Slider slider;
    public TMP_Text maxHealthText;
    public TMP_Text attackDamageText;
    public TMP_Text stageText;
    
    public TMP_Text moneyText;
    public TMP_Text speedText;
    public TMP_Text deathStageText;
    
    void Start()
    {
        SetStage($"{GameManager.Instance.GameStage}-{GameManager.Instance.GameLevel}");
    }
    public void SetStage(string stage)
    {
        stageText.text = $"{stage}";
        deathStageText.text = $"기록: {stage}";
    }
    
    public void SetMaxHealth(int health)
    {
        maxHealthText.text = $"{health}";
        slider.maxValue = health;
    }

    public void SetCurrentHealth(int health)
    {
        slider.value = health;
    }
    public void SetAttackDamage(int attackDamage)
    {
        attackDamageText.text = $"{attackDamage}";
    }

    public void SetCurrentMoney(int money)
    {
        moneyText.text = $"{money}";
    }

    public void SetCurrentMoveSpeed(int speed)
    {
        speedText.text = $"{speed}";
    }
    
    
}
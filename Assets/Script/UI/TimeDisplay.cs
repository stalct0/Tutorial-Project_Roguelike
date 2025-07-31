using UnityEngine;
using TMPro;

public class TimeDisplay : MonoBehaviour
{
    public TMP_Text timeText;

    private float timer = 0f;

    void Update()
    {
        // 시간 누적
        timer += Time.deltaTime;

        // 분, 초 계산
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);

        // 텍스트 표시
        timeText.text = $"{minutes:00}:{seconds:00}";
    }
}

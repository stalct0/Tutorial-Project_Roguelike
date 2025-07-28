using UnityEngine;
using TMPro;

public class StatDisplay : MonoBehaviour
{
    public TMP_Text statText;

    void Start()
    {
        SetStat(99);  // ✔️ 이게 맞는 함수
    }

    public void SetStat(int power)
    {
        statText.text = $"{power}";
    }
}
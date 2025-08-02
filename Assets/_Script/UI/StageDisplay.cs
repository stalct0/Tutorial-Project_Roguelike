using UnityEngine;
using TMPro;

public class StageDisplay : MonoBehaviour
{
    public TMP_Text stageText;

    void Start()
    {
        SetStage("3-2");  // ✔️ 이게 맞는 함수
    }

    public void SetStage(string stage)
    {
        stageText.text = $"{stage}";
    }
}
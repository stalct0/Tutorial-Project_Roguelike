using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public TMP_Text HighScoreText;
    void Start()
    {
        // 저장된 최고 기록 불러와서 표시
        HighScoreText.text = $"최고 기록: {HighScore.GetAsText()}";
    }
    public void OnClickNormalMode ()
    {
        GameManager.Instance.SetGameMode(GameManager.GameMode.Normal);
        GameManager.Instance.LoadScene("CharSelect");
    }

    public void OnClickInfinityMode()
    {
        GameManager.Instance.SetGameMode(GameManager.GameMode.Infinite);
        GameManager.Instance.LoadScene("CharSelect");
    }

    public void OnClickCredit()
    {
        GameManager.Instance.LoadScene("Credit");
    }


    public void OnClickQuit()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    
}

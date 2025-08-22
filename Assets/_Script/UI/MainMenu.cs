using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void OnClickNormalMode ()
    {
        GameManager.Instance.LoadScene("CharSelect");
    }

    public void OnClickInfinityMode()
    {
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

using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void OnClickNew ()
    {
        GameManager.Instance.LoadScene("CharSelect");
    }

    public void OnClickLoad()
    {
        
    }

    public void OnClickOption()
    {
        GameManager.Instance.LoadScene("Option");
    }


    public void OnClickQuit()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}

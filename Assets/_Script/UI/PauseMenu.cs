using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    void Awake()
    {
        this.gameObject.SetActive(false);
        GameManager.Instance.pauseMenuUI = this.gameObject;
    }
    
    public void OnRetryClicked()
    {
        GameManager.Instance.ResumeGame();
        GameManager.Instance.DestroyPlayer();
        GameManager.Instance.LoadScene("CharSelect");
    }
}

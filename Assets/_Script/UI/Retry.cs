using UnityEngine;
using UnityEngine.UI;
public class Retry : MonoBehaviour
{
    void Awake()
    {
        this.gameObject.SetActive(false);
        GameManager.Instance.gameOverUI = this.gameObject;
    }
    
    public void OnRetryClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance.LoadScene("CharSelect");
    }
}
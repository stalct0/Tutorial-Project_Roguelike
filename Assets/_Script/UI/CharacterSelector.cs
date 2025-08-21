using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    public void SelectRed()
    {
        GameManager.Instance.LoadScene("Lobby");
        GameManager.Instance.GameLevel = 1;
        GameManager.Instance.GameStage = 1;
    }

    public void SelectBlue()
    {
        //GameManager.Instance.LoadScene("Level");
    }

    public void SelectGreen()
    {
        //GameManager.Instance.LoadScene("Level");
    }

    public void SelectYellow()
    {
        //GameManager.Instance.LoadScene("Level");
    }
}

using System;
using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Exit();
        }
    }

    public void SelectRed()
    {
        GameManager.Instance.SetPlayerClass(PlayerClass.Music);
        GameManager.Instance.LoadScene("Lobby");
        GameManager.Instance.GameLevel = 1;
        GameManager.Instance.GameStage = 1;
    }

    public void SelectBlue()
    {
        GameManager.Instance.SetPlayerClass(PlayerClass.Software);
        GameManager.Instance.LoadScene("Lobby");
        GameManager.Instance.GameLevel = 1;
        GameManager.Instance.GameStage = 1;
    }

    public void SelectGreen()
    {
        //GameManager.Instance.LoadScene("Level");
    }

    public void SelectYellow()
    {
        //GameManager.Instance.LoadScene("Level");
    }

    public void Exit()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Menu);
        GameManager.Instance.LoadScene("MainMenu");
    }
}

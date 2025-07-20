using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    void Update()
    {
        
    }
    
    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
    }
    
    
    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    public void PauseGame()
    {
        Time.timeScale = 0f;
        SetGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        SetGameState(GameState.Playing);
    }
    
    
}

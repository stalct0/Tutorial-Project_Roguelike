using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } //Singleton
    public GameObject pauseMenuUI;
    public GameObject gameOverUI;
    
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver,
        Shop
    }

    public GameState CurrentState { get; private set; }

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

    void Start()
    {

    }

    void Update()
    {
        switch (CurrentState)
        {
            case GameState.Playing:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGame();
                }

                break;

            case GameState.Paused:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ResumeGame();
                }
                break;
        }
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Level") 
        {
            SetGameState(GameState.Playing);
        }
    }
    public void RegisterPlayer(PlayerStats stats)
    {
        stats.onDie.AddListener(GameOver);
    }

    void GameOver()
    {
        gameOverUI.SetActive(true);
        SetGameState(GameState.GameOver);
        Time.timeScale = 0f;
    }

    public void SetGameState(GameState newState)
        {
            CurrentState = newState;
        }


        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void PauseGame()
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
        }

        public void ResumeGame()
        {
            pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;
            SetGameState(GameState.Playing);
        }

        public void RestartGame()
        {

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }


    }

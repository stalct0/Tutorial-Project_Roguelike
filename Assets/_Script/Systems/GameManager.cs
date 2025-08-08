using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } //Singleton
    public GameObject pauseMenuUI;
    public GameObject gameOverUI;
    
    [NonSerialized] public int GameStage = 1;
    [NonSerialized] public int GameLevel = 1;
    
    [Header("Player")]
    public GameObject playerPrefab;
    public GameObject playerInstance {get; private set; }   // 생성 후 유지 
    // 캐싱할 레퍼런스들
    public PlayerController PC { get; private set; }
    public PlayerStats     PStats { get; private set; }
    public PlayerInventory PInventory { get; private set; }
    public Rigidbody2D     PRB { get; private set; }
    
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
            case GameState.Shop:
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

    public void SpawnOrMovePlayer(Vector3 spawnPos, Tilemap ladderTilemap)
    {
        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            DontDestroyOnLoad(playerInstance);
            CachePlayerComponents(); 
        }
        else
        {
            playerInstance.transform.position = spawnPos;
            if (PRB) PRB.linearVelocity = Vector2.zero;
        }

        // 사다리/카메라 참조 재결합
        if (PC) PC.ladderTilemap = ladderTilemap;

        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam) vcam.Follow = playerInstance.transform;
        
        BindPlayerUI();
    }

    public void ResetPlayerPosition()
    {
        playerInstance.transform.position = Vector3.zero;
        PC.ladderTilemap = null;
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
        
        public void NextStage()
        {
            GameLevel++;
            if (GameLevel > 5)
            {
                GameStage++;
                GameLevel = 1;
            }
        }
        
        void BindPlayerUI()
        {
            if (PStats == null) return;

            // 씬에 새로 생성된 StatDisplay 찾기 (이름말고 타입으로)
            var sd = FindFirstObjectByType<StatDisplay>();  // 2022+ 권장
            if (sd == null) return;

            // PlayerStats에 UI 연결
            PStats.statDisplay = sd;

            // 현재 스탯을 UI에 밀어넣기
            sd.SetMaxHealth(PStats.maxHealth);
            sd.SetHealth(PStats.currentHealth);
            sd.SetStat(PStats.attackDamage);
            sd.SetMoney(PStats.Money);
        }
        
        void CachePlayerComponents()
        {
            PC     = playerInstance.GetComponent<PlayerController>();
            PStats = playerInstance.GetComponent<PlayerStats>();
            PRB    = playerInstance.GetComponent<Rigidbody2D>();
        }


    }

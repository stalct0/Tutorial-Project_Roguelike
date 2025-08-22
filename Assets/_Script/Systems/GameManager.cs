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
    public PlayerItemInteractor PItemInteractor { get; private set; }
    public Rigidbody2D     PRB { get; private set; }
    
    // 플레이어 생성 완료 신호 이벤트
    public event System.Action<PlayerInventory> OnInventoryReady;
    
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Inventory,
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
            case GameState.Inventory:
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

        if (scene.name == "Lobby")
        {
            SetGameState(GameState.Playing);
        }

        if (scene.name == "Boss")
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
        if (PC) PC.ladderTilemap = null;
    }

    private bool resetLadder = false;

    public void ResetPlayerladder()
    {
        if (resetLadder == false)
        {
            resetLadder = true;
            Tilemap ladderTilemap = GameObject.Find("LadderTilemap").GetComponent<Tilemap>();
            if (PC) PC.ladderTilemap = ladderTilemap;
        }
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

       public void NextStage()
        {
            GameLevel++;
            if (GameLevel == 5)
            {
                LoadScene("Boss");
            }
            else
            {
                LoadScene("Level");
            }

            if (GameLevel == 6)
            {
                GameStage++;
                GameLevel = 1;
            }
        }
        
        void BindPlayerUI()
        {
            // 씬에 새로 생성된 StatDisplay 찾기 (이름말고 타입으로)
            var sd = FindFirstObjectByType<StatDisplay>();

            // PlayerStats에 UI 연결
            PStats.statDisplay = sd;
            
            
            // 현재 스탯을 UI에 밀어넣기
            sd.SetMaxHealth(PStats.maxHealth);
            sd.SetCurrentHealth(PStats.currentHealth);
            sd.SetAttackDamage(PStats.currentAttackDamage);
            sd.SetCurrentMoney(PStats.currentMoney);
            
            OnInventoryReady?.Invoke(PInventory);
            
          var prompt = FindFirstObjectByType<InteractUI>(); 
          if (PItemInteractor != null && prompt != null)
          {
              PItemInteractor.promptUI = prompt;
              prompt.gameObject.SetActive(false); // 시작은 꺼둠
              PItemInteractor.screenPrompt = PItemInteractor.promptUI.GetComponent<RectTransform>();

          }
          
        }
        
        void CachePlayerComponents()
        {
            PC     = playerInstance.GetComponent<PlayerController>();
            PStats = playerInstance.GetComponent<PlayerStats>();
            PInventory= playerInstance.GetComponent<PlayerInventory>();
            PItemInteractor = playerInstance.GetComponent<PlayerItemInteractor>();
            PRB    = playerInstance.GetComponent<Rigidbody2D>();
        }

        public void DestroyPlayer()
        {
            Destroy(playerInstance);
            playerInstance = null;
        }

    }

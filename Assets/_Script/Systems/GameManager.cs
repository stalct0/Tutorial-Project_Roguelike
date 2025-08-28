using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public enum PlayerClass { Music, Software }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } //Singleton
    public GameObject pauseMenuUI;
    public GameObject gameOverUI;

    [NonSerialized] public int GameStage = 1;
    [NonSerialized] public int GameLevel = 1;
    [NonSerialized] public float LevelCoefficient = 1f;
    [NonSerialized] public float LevelCoRate = 0.3f;



    [Header("Player")] public GameObject playerPrefab;

    [Header("Player Prefabs (by Class)")] // ★ ADD
    [SerializeField]
    private GameObject musicPlayerPrefab; // 기타 캐릭터 프리팹

    [SerializeField] private GameObject softwarePlayerPrefab; // 소프트웨어 캐릭터 프리팹

    public PlayerClass SelectedClass { get; private set; } = PlayerClass.Music; // ★ ADD
    const string KEY_PLAYER_CLASS = "player_class"; // ★ ADD


    [Header("BGM")] [SerializeField] private AudioSource bgmSource; // GameManager 오브젝트에 붙인 AudioSource 연결
    [SerializeField] private AudioClip mainMenuBGM; // MainMenu 전용
    [SerializeField] private AudioClip lobbyBGM; // Lobby 전용
    [SerializeField] private AudioClip fieldBGM; // Level & Shop 공용 (끊김 없이 유지)
    [SerializeField] private AudioClip bossBGM; // Boss 전용

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;  // 효과음 전용
    [SerializeField] private AudioClip sfxJump;      // 점프
    [SerializeField] private AudioClip sfxPickup;    // 아이템 획득
    [SerializeField] private AudioClip sfxAttack;    // 공격(휘두르기)

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (!clip || !sfxSource) return;
        sfxSource.PlayOneShot(clip, volume); // 겹쳐도 안전하게 1회 재생
    }

    public void SfxJump(float volume = 1f)   => PlaySFX(sfxJump, volume);
    public void SfxPickup(float volume = 1f) => PlaySFX(sfxPickup, volume);
    public void SfxAttack(float volume = 1f) => PlaySFX(sfxAttack, volume);
    

    public GameObject playerInstance { get; private set; } // 생성 후 유지 

    // 캐싱할 레퍼런스들
    public PlayerController PC { get; private set; }
    public PlayerStats PStats { get; private set; }
    public PlayerInventory PInventory { get; private set; }
    public PlayerItemInteractor PItemInteractor { get; private set; }
    public Rigidbody2D PRB { get; private set; }



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

    public enum GameMode
    {
        Normal,
        Infinite
    }

    public GameState CurrentState { get; private set; }

    public GameMode CurrentGameMode { get; private set; }

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
        CurrentState = GameState.Menu;
        UpdateBGMForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        switch (CurrentState)
        {
            case GameState.Menu:
                break;

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
        if (scene.name == "Level" || scene.name == "Lobby" || scene.name == "Boss")
        {
            SetGameState(GameState.Playing);

            // ★ 지금 진입한 씬 기준으로 하이스코어 갱신
            HighScore.TrySet(GameStage, GameLevel);
        }

        UpdateBGMForScene(scene.name);
    }

    public void SetPlayerClass(PlayerClass cls)
    {
        SelectedClass = cls;
        PlayerPrefs.SetInt(KEY_PLAYER_CLASS, (int)cls);
        PlayerPrefs.Save();
    }

    public void RegisterPlayer(PlayerStats stats)
    {
        stats.onDie.AddListener(GameOver);
    }

    public void SpawnOrMovePlayer(Vector3 spawnPos, Tilemap ladderTilemap)
    {
        if (playerInstance == null)
        {
            GameObject prefab =
                (SelectedClass == PlayerClass.Software && softwarePlayerPrefab != null) ? softwarePlayerPrefab :
                (musicPlayerPrefab != null) ? musicPlayerPrefab :
                playerPrefab; // 마지막 안전망

            playerInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
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
        HighScore.TrySet(GameStage, GameLevel);
        gameOverUI.SetActive(true);
        SetGameState(GameState.GameOver);
        Time.timeScale = 0f;
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
    }

    public void SetGameMode(GameMode newGameMode)
    {
        CurrentGameMode = newGameMode;
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
        LevelCoefficient = LevelCoefficient + LevelCoRate;
        if (CurrentGameMode == GameMode.Normal)
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
                LoadScene("Ending");
            }
        }
        else if (CurrentGameMode == GameMode.Infinite)
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
    }

    void BindPlayerUI()
    {
        // 씬에 새로 생성된 StatDisplay 찾기 (이름말고 타입으로)
        var sd = FindFirstObjectByType<StatDisplay>();

        // PlayerStats에 UI 연결
        PStats.statDisplay = sd;


        // 현재 스탯을 UI에 밀어넣기
        if (sd != null)
        {
            sd.SetMaxHealth(PStats.maxHealth);
            sd.SetCurrentHealth(PStats.currentHealth);
            sd.SetAttackDamage(PStats.currentAttackDamage);
            sd.SetCurrentMoney(PStats.currentMoney);
            sd.SetCurrentMoveSpeed(PStats.currentMoveSpeed);
        }

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
        PC = playerInstance.GetComponent<PlayerController>();
        PStats = playerInstance.GetComponent<PlayerStats>();
        PInventory = playerInstance.GetComponent<PlayerInventory>();
        PItemInteractor = playerInstance.GetComponent<PlayerItemInteractor>();
        PRB = playerInstance.GetComponent<Rigidbody2D>();
    }

    public void DestroyPlayer()
    {
        Destroy(playerInstance);
        playerInstance = null;
    }

    public void StartGame()
    {
        LevelCoefficient = 1;
    }

    private void UpdateBGMForScene(string sceneName)
    {
        AudioClip target = null;

        if (sceneName == "MainMenu" || 
            sceneName == "CharSelect" || 
            sceneName == "Lobby" || 
            sceneName == "Credit" || 
            sceneName == "Ending") 
            target = mainMenuBGM;
        
            else if (sceneName == "Level" || sceneName == "Shop")
                target = fieldBGM;        // Level↔Shop은 같은 클립 → 재시작 없음
            else if (sceneName == "Boss")
                target = bossBGM;

            PlayBGM(target, 0.25f); // 살짝 페이드(선택) — 0으로 주면 즉시 전환
        }
        public void PlayBGM(AudioClip clip, float fadeSeconds = 0.25f)
        {
            if (bgmSource == null)
                return;

            // 같은 곡 이미 재생 중이면 그대로 둠 (Level↔Shop 이동 시 끊김 방지 핵심)
            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            StopAllCoroutines();
            StartCoroutine(SwapBGM(clip, fadeSeconds));
        }

        private System.Collections.IEnumerator SwapBGM(AudioClip newClip, float fadeSeconds)
        {
            // 타임스케일 0(메뉴/일시정지)에서도 부드럽게 페이드되도록 unscaled 사용
            float startVol = bgmSource.volume;
            fadeSeconds = Mathf.Max(0f, fadeSeconds);

            if (bgmSource.isPlaying && bgmSource.clip != null && fadeSeconds > 0f)
            {
                // Fade-out
                float t = 0f;
                while (t < fadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeSeconds);
                    yield return null;
                }
                bgmSource.Stop();
                bgmSource.volume = 0f;
            }
            else
            {
                bgmSource.Stop();
                bgmSource.volume = 0f;
            }

            // 교체 & 재생
            bgmSource.clip = newClip;
            if (newClip == null)
                yield break;

            bgmSource.loop = true;
            bgmSource.Play();

            // Fade-in
            if (fadeSeconds > 0f)
            {
                float t = 0f;
                while (t < fadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, startVol, t / fadeSeconds);
                    yield return null;
                }
            }
            bgmSource.volume = startVol;
        }
    }

using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 사용

public class BossExit : MonoBehaviour
{
    [Header("Lock / Unlock")]
    [SerializeField] bool startLocked = true;           // 시작 시 잠금 여부
    [SerializeField] Collider2D blockingCollider;       // 길 막는 콜라이더(Trigger 아님)
    [SerializeField] Collider2D triggerCollider;        // 상호작용 범위(Trigger = ON)
    [SerializeField] GameObject lockedVfx;              // 잠금 상태 표시(옵션)
    [SerializeField] GameObject unlockedVfx;            // 해제 상태 표시(옵션)

    private bool isLocked = true;
    private bool playerInRange = false;

    void Awake()
    {
        ApplyLocked(startLocked);
    }

    void OnEnable()
    {
        // 보스 처치 시 자동 해제
        GameEvents.BossDefeated += OnBossDefeated;

        // 이미 보스가 죽은 뒤에 문이 활성화될 수도 있으므로 플래그 확인
        if (GameEvents.BossDefeatedFlag)
            ApplyLocked(false);
    }

    void OnDisable()
    {
        GameEvents.BossDefeated -= OnBossDefeated;
    }

    void OnBossDefeated()
    {
        ApplyLocked(false);
    }

    // --- 외부에서 직접 호출도 가능하도록 공개 메서드 제공 ---
    public void Unlock() => ApplyLocked(false);
    public void Lock()   => ApplyLocked(true);

    // 잠금/해제 실제 적용부 (활성/비활성 한 곳에서만 처리)
    private void ApplyLocked(bool locked)
    {
        isLocked = locked;

        if (blockingCollider) blockingCollider.enabled = locked;
        if (triggerCollider)  triggerCollider.enabled  = !locked;

        if (lockedVfx)   lockedVfx.SetActive(locked);
        if (unlockedVfx) unlockedVfx.SetActive(!locked);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLocked && other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    void Update()
    {
        if (isLocked) return;

        // I 키로 상호작용 (새 Input System)
        if (playerInRange && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Shop);
            GameManager.Instance.LoadScene("Shop");
        }
    }



}

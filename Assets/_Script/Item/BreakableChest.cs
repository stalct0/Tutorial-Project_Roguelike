using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 공격(또는 다른 히트)에 맞으면 깨지는 상자.
/// 깨질 때 LootTable에서 아이템을 뽑아 ItemPickup 프리팹을 생성한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BreakableChest : MonoBehaviour, IHittable
{
    [Header("Health")]
    public int maxHP = 10;
    public int currentHP;

    [Header("Hit/I-Frame")]
    public float invincibleSecOnHit = 0.05f; // 중복타 방지용 짧은 i-frame
    private bool isInvincible;
    private float invTimer;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer; // 비워두면 자동 할당
    public Sprite brokenSprite;                             // 깨진 후 바꿀 스프라이트(선택)
    public GameObject breakVFX;                             // 깨질 때 VFX 프리팹(선택)
    public float destroyAfter = 1.0f;                       // 깨진 뒤 제거까지 대기(0이면 바로 유지)

    [Header("Loot")]
    public LootTable lootTable;
    public GameObject pickupPrefabFallback;     // ItemPickup 프리팹(아이템별 프리팹 없을 때)
    public Vector2Int spawnCountRange = new Vector2Int(1, 1); // 몇 개 스폰할지
    public bool includeOnPickup = true;         // OnPickup 아이템도 드랍 허용할지
    public int currentStageForFilter = 0;       // 필요시 스테이지 필터

    [Header("Spawn Physics")]
    public Vector2 randomOffsetRadius = new Vector2(0.2f, 0.05f);   // 가로/세로 랜덤 오프셋
    public Vector2 impulseXRange = new Vector2(-0.8f, 0.8f);
    public Vector2 impulseYRange = new Vector2(1.0f, 1.8f);

    private bool isBroken;
    private Collider2D solidCol;

    void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        solidCol = GetComponent<Collider2D>();
        currentHP = maxHP;
    }

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        solidCol = GetComponent<Collider2D>();
        currentHP = Mathf.Max(1, maxHP);
    }

    void Update()
    {
        if (isInvincible)
        {
            invTimer -= Time.deltaTime;
            if (invTimer <= 0f) isInvincible = false;
        }
    }

    // ─────────────────────────────────────────────────────

    public void TakeHit(DamageEvent e)
    {
        if (isBroken) return;
        if (isInvincible) return;

        // 대미지 감소
        currentHP -= Mathf.Max(0, e.damage);

        // 간단한 히트 플래시(선택)
        if (spriteRenderer) StartCoroutine(HitFlashCo());

        // i-frame
        isInvincible = true;
        invTimer = invincibleSecOnHit;

        if (currentHP <= 0)
        {
            Break(e);
        }
    }

    private IEnumerator HitFlashCo()
    {
        if (!spriteRenderer) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = new Color(1f, 0.6f, 0.6f, orig.a);
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = orig;
    }

    // ─────────────────────────────────────────────────────

    private void Break(DamageEvent source)
    {
        if (isBroken) return;
        isBroken = true;

        // 충돌 해제
        if (solidCol) solidCol.enabled = false;

        // 비주얼 전환
        if (breakVFX)
        {
            Instantiate(breakVFX, transform.position, Quaternion.identity);
        }
        if (spriteRenderer && brokenSprite)
        {
            spriteRenderer.sprite = brokenSprite;
        }

        // 루팅
        SpawnLoot();

        // 파괴 또는 유지
        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);
        else
            Destroy(gameObject); // 바로 삭제
    }

    private void SpawnLoot()
    {
        if (lootTable == null) return;

        int count = Mathf.Clamp(Random.Range(spawnCountRange.x, spawnCountRange.y + 1), 0, 32);
        for (int i = 0; i < count; i++)
        {
            var def = lootTable.Roll(currentStageForFilter, includeOnPickup);
            if (def == null) continue;
            if (pickupPrefabFallback == null) continue;
            GameObject prefab = pickupPrefabFallback;

            // 약간의 랜덤 오프셋
            Vector3 pos = transform.position
                          + new Vector3(Random.Range(-randomOffsetRadius.x, randomOffsetRadius.x),
                              Random.Range(0f, randomOffsetRadius.y), 0f);

            var go = Instantiate(prefab, pos, Quaternion.identity);

            // 아이템 정보 세팅
            var pickup = go.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                int rolled = Random.Range(def.minRoll, def.maxRoll + 1); // ★ ADD
                pickup.Set(def, rolled);                                 // ★ CHG
            }
            else
            {
                // 안전망: SpriteRenderer 직접 세팅
                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) { sr.enabled = (def.icon != null); sr.sprite = def.icon; }
            }

            // 살짝 튀기기
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                float ix = Random.Range(impulseXRange.x, impulseXRange.y);
                float iy = Random.Range(impulseYRange.x, impulseYRange.y);
                rb2d.linearVelocity = Vector2.zero;
                rb2d.AddForce(new Vector2(ix, iy), ForceMode2D.Impulse);
            }
        }
    }
}
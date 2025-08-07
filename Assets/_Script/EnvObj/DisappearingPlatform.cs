using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    private float disappearDelay = 0.7f; // 사라질 때까지 대기 시간

    private bool isSteppedOn = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    
    private Vector2 checkSize = new Vector2(0.7f, 0.1f); // 발판 위 폭, 두께
    private Vector2 checkOffset = new Vector2(0f, 0.3f); // 발판 중심에서 위로
    
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (isSteppedOn) return;

        // 발판 위에 플레이어가 올라와 있는지 체크 (플레이어 Layer/Tag 조정)
        Vector2 center = (Vector2)transform.position + checkOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, checkSize, 0f, LayerMask.GetMask("Player"));
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                isSteppedOn = true;
                StartCoroutine(DisappearCoroutine());
                break;
            }
        }
    }
    

    System.Collections.IEnumerator DisappearCoroutine()
    {
        // 2초 대기 (여기서 이펙트/점멸 등 넣어도 됨)
        yield return new WaitForSeconds(disappearDelay);

        // 사라짐(일단 비활성화)
        if (spriteRenderer) spriteRenderer.enabled = false;
        if (col) col.enabled = false;

        // 완전히 파괴하려면 Destroy(gameObject); 해도 됨
        // Destroy(gameObject);
    }
    void OnDrawGizmosSelected()
    {
        Vector2 center = (Vector2)transform.position + checkOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, checkSize);
    }
}

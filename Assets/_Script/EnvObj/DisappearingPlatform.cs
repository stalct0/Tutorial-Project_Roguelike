using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    public float disappearDelay = 2.0f; // 사라질 때까지 대기 시간

    private bool isSteppedOn = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어가 발판 위에 올라섰는지 확인 (Tag, Layer 등으로 필터)
        if (!isSteppedOn && collision.collider.CompareTag("Player"))
        {
            isSteppedOn = true;
            StartCoroutine(DisappearCoroutine());
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
}

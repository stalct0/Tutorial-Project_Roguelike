using UnityEngine;


//소주병(프로젝타일) 정의
public class SojuProjectile : MonoBehaviour, IHittable
{
    public Rigidbody2D rb;
    private bool _dead; // 중복 파괴 방지

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    
    public void TakeHit(DamageEvent e)
    {
        if (_dead) return;
        _dead = true;
        Debug.Log("parrrrrrry");
        // 플레이어 공격이 닿으면 패링됨
        Destroy(gameObject);
    }
    
    
    // 적 피격 시 데미지 들어감
    // "TakeDamage" 플레이어 AI 스크립트에서 호출해야됨
    // 적이 피격되면 사라짐
    // 플랫폼 피격 시 사라짐
    // 땅 피격 시 사라짐 

    
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Destroy on contact with player
        
        if (collision.gameObject.CompareTag("Player"))
        {
            var playerStats = collision.gameObject.GetComponent<PlayerStats>();
            if (playerStats != null) playerStats.TakeDamage(20);
            if (!_dead) Destroy(gameObject);
            return;
        }
        
        
        // Destroy on contact with platform/ground
        if (
            collision.gameObject.CompareTag("Ground") || 
            collision.gameObject.CompareTag("Platform") || 
            collision.gameObject.CompareTag("OneWayPlatform")
            )
        {
            if (!_dead) Destroy(gameObject);
        }
        
    }

    // 2차함수 꼴로 투사체가 날아감. 
    public void Launch(Vector3 targetPosition)
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y);
        Vector2 start = transform.position;
        Vector2 end = targetPosition;
        float dx = end.x - start.x;
        float dy = end.y - start.y;

        // 더 위에 있는 적 과 아래에 있는 적 상대 투사체 초기 각도 변화
        float baseAngle = 45f;
        if (Mathf.Abs(dx) > 0.01f)
        {
            float heightRatio = Mathf.Clamp(dy / Mathf.Abs(dx), -1f, 1f);
            baseAngle += heightRatio * 15f; // up to +/-15° based on relative height
        }
        baseAngle = Mathf.Clamp(baseAngle, 30f, 65f);

        // 초기 각도에 살짝 변화를 줌. 
        float angleVariation = Random.Range(-6f, 6f);
        float angle = (baseAngle + angleVariation) * Mathf.Deg2Rad;

        // 초기속도 계산 (이차함수꼴)
        float cosAngle = Mathf.Cos(angle);
        float sinAngle = Mathf.Sin(angle);
        float distance = Mathf.Abs(dx);
        float denominator = distance * Mathf.Tan(angle) - dy;
        float speed = 0f;
        if (denominator > 0.01f)
        {
            speed = Mathf.Sqrt((gravity * distance * distance) / (2f * cosAngle * cosAngle * denominator));
        }
        else
        {
            // Fallback: use a reasonable default speed if denominator is too small
            speed = 10f;
        }
        // 최대 속도 제한
        speed = Mathf.Clamp(speed, 6f, 18f);

        // 벡터화
        float vx = speed * cosAngle * Mathf.Sign(dx);
        float vy = speed * sinAngle;
        rb.linearVelocity = new Vector2(vx, vy);

        // 소주병 무작위 회전
        float randomAngularVelocity = Random.Range(-360f, 360f);
        rb.angularVelocity = randomAngularVelocity;

        // 3 초 후 파괴 (if not already destroyed)
        Destroy(gameObject, 3f);
    }
}

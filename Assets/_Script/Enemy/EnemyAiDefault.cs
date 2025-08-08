using System;
using UnityEngine;


// 적 AI 상태 정의 
public enum EnemyStateD {
    Patrol,
    Reset,
}


// 적 AI 정의 
public class EnemyAiDefault : MonoBehaviour
{
    
    //적 스탯 정의 
    // 체력
    public int health = 100; 

    private float frontCheckOffset = 0.3f;   // 몸 중심에서 살짝 앞
    private float frontCheckDist   = 0.1f;   // 전방 레이 길이
    private float sameLevelYTolerance = 0.6f;// 높이 차 허용(같은 층만 충돌로 간주)
    
    public LayerMask enemyLayerMask;        // "Enemy" 레이어만 포함

    // 패드롤 논리 변수 정의 
    private float patrolEdgeOffset = 0.3f; // Distance from edge to stop
    public EnemyStateD currentState = EnemyStateD.Patrol;
    
    // 플레이어 변수 정의 (사실 unity inspector 에서 할당된 값이 우선 적용됨. )
    public Transform player;
    
    private float moveSpeed = 1f;
    private float currentDirection = 1f;

    //private Animator animator;
    private Rigidbody2D rb;
    // Removed unused isFacingRight field
    // Removed unused isHit field
    
    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.transform;
        }
        //animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        // Freeze rotation so enemy doesn't rotate
        rb.freezeRotation = true;
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyStateD.Patrol:
                Patrol();
                break;
        }
    }


    //패트롤 
    void Patrol()
    {
        //animator.SetBool("isWalking", true);
        
        // 패트롤 움직임 및 
        float direction = currentDirection;
        
        // 전방 몬스터 충돌 체크 → 있으면 반전
        if (IsEnemyInFront())
        {
            currentDirection = -currentDirection;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        //엣지 감지
        Vector2 edgeCheckOrigin = transform.position + new Vector3(direction * patrolEdgeOffset, 0f, 0f);
        int platformLayerMask = LayerMask.GetMask("Ground"); // add your platform layers
        RaycastHit2D[] hits = new RaycastHit2D[4];
        int hitCount = Physics2D.RaycastNonAlloc(edgeCheckOrigin, Vector2.down, hits, 0.5f, platformLayerMask);

        Debug.DrawRay(edgeCheckOrigin, Vector2.down * 0.5f, Color.red, 2f);
        RaycastHit2D validHit = default;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].collider != null && hits[i].collider.gameObject != this.gameObject)
            {
                validHit = hits[i];
                break;
            }
        }
        if (validHit.collider == null)
        {
            // 엣지에 도달 → 방향 반전
            currentDirection = -currentDirection; // (+1 ↔ -1)
            rb.linearVelocity = Vector2.zero;     // (선택) 잠시 멈춤
            return;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }
    

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            // Handle enemy death (destroy, play animation, etc.)
            Destroy(gameObject);
        }
        else
        {
            // Optional: play hit animation or effect
        }
    }
    
    bool IsEnemyInFront()
    {
        float dir = Mathf.Sign(currentDirection); // +1 오른쪽, -1 왼쪽
        // 가슴 높이 정도에서 전방으로 쏘는 레이
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * frontCheckOffset, 0f);
        Vector2 dirVec = new Vector2(dir, 0f);

        // 레이캐스트
        var hit = Physics2D.Raycast(origin, dirVec, frontCheckDist, enemyLayerMask);

        // 디버그
        Debug.DrawRay(origin, dirVec * frontCheckDist, Color.cyan, 0.05f);

        if (hit.collider == null) return false;
        if (hit.collider.attachedRigidbody == rb) return false; // 자기 자신 제외

        // 같은 층(높이)만 적으로 간주 (경사/난간에서 위아래 다른 층은 무시)
        float myY = GetComponent<Collider2D>().bounds.center.y;
        float otherY = hit.collider.bounds.center.y;
        if (Mathf.Abs(myY - otherY) > sameLevelYTolerance) return false;

        return true;
    }
    
}

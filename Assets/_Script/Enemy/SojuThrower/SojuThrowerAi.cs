using System;
using UnityEngine;


// 적 AI 상태 정의 
public enum EnemyState {
    Patrol,
    Agro,
    MoveToRange,
    AttackPrepare,
    Attack,
    Reset,
}


// 적 AI 정의 
public class SojuThrowerAI : MonoBehaviour
{

    //적 스탯 정의 
    // 체력
    public int health = 100; 




    // 플레이어가 사거리를 넘어가면 몇초 후 패트롤로 돌아감. 
    private float outOfRangeTimer = 0f;
    public float outOfRangeDuration = 2f; // seconds to wait before returning to patrol



    // 패드롤 논리 변수 정의 
    private float patrolEdgeOffset = 0.3f; // Distance from edge to stop
    public EnemyState currentState = EnemyState.Patrol;


    // 플레이어 변수 정의 (사실 unity inspector 에서 할당된 값이 우선 적용됨. )
    public Transform player;
    
    private float moveSpeed = 1f;
    private float agroRange = 4f;
    private float attackRange = 5f;
    private float prepareTime = 1.0f;

    private float currentDirection = 1f;

    //private Animator animator;
    private Rigidbody2D rb;
    // Removed unused isFacingRight field
    // Removed unused isHit field
    private float prepareTimer;

    // 던지는 소주병  불러오기
    public GameObject sojuPrefab; // Assign this in the Unity Inspector

    
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
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Agro:
                Agro();
                break;
            case EnemyState.MoveToRange:
                MoveToPlayer();
                break;
            case EnemyState.AttackPrepare:
                AttackPrepare();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Reset:
                ResetAfterAttack();
                break;
        }
    }


    //패트롤 
    void Patrol()
    {
        //animator.SetBool("isWalking", true);


        // 좌우 순회 로직 
        if (player == null)
        {
            // No player, never aggro
        }
        else
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (!float.IsNaN(dist) && dist < agroRange)
            {
                //animator.SetTrigger("isStunned");
                currentState = EnemyState.Agro;
                return;
            }
        }

        // 패트롤 움직임 및 엣지 감지
        float direction = currentDirection;
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

    void Agro()
    {
        // 어그로 끌리고 잠시 정지
        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(GoToMoveState), 0.5f);
    }

    void GoToMoveState()
    {
        if (player == null)
        {
            currentState = EnemyState.Patrol;
        }
        else
        {
            currentState = EnemyState.MoveToRange;
        }
    }

    void MoveToPlayer()
    {
        //animator.SetBool("isWalking", true);
        if (player == null)
        {
            // No player, return to patrol
            currentState = EnemyState.Patrol;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        float direction = player.position.x - transform.position.x;
        float distanceToPlayer = Mathf.Abs(direction);
        float moveDir = Mathf.Sign(direction);

        // 움직이기 전 엣지 감지 (떨어지지 않기 위해)
        Vector2 edgeCheckOrigin = transform.position + new Vector3(moveDir * patrolEdgeOffset, 0f, 0f);
        int platformLayerMask = LayerMask.GetMask("Ground");
        RaycastHit2D[] hits = new RaycastHit2D[4];
        int hitCount = Physics2D.RaycastNonAlloc(edgeCheckOrigin, Vector2.down, hits, 0.5f, platformLayerMask);
        bool nearEdge = true;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].collider != null && hits[i].collider.gameObject != this.gameObject)
            {
                nearEdge = false;
                break;
            }
        }

        if (nearEdge)
        {
            rb.linearVelocity = Vector2.zero;
            if (distanceToPlayer <= attackRange)
            {
                // Allow attacking even if near edge
                if (player != null)
                {
                    currentState = EnemyState.AttackPrepare;
                    prepareTimer = prepareTime;
                }
                else
                {
                    currentState = EnemyState.Patrol;
                }
            }
            else
            {
                currentState = EnemyState.Patrol;
            }
            outOfRangeTimer = 0f;
            return;
        }

        // Always check attack range and transition to AttackPrepare if in range
        if (distanceToPlayer <= attackRange && !nearEdge)
        {
            rb.linearVelocity = Vector2.zero;
            if (player != null)
            {
                currentState = EnemyState.AttackPrepare;
                prepareTimer = prepareTime;
            }
            else
            {
                currentState = EnemyState.Patrol;
            }
            return;
        }
        // If not in attack range, move toward player
        if (distanceToPlayer > attackRange)
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
        }

        // 사거리 조정 타이머 로직
        if (distanceToPlayer > agroRange)
        {
            outOfRangeTimer += Time.deltaTime;
            if (outOfRangeTimer >= outOfRangeDuration)
            {
                currentState = EnemyState.Patrol;
                outOfRangeTimer = 0f;
            }
        }
        else
        {
            outOfRangeTimer = 0f;
        }
    }

    void AttackPrepare()
    {
        //animator.SetBool("isWalking", false);
        //animator.SetBool("isPreparing", true);
        if (player == null)
        {
            // No player, return to patrol
            currentState = EnemyState.Patrol;
            return;
        }
        prepareTimer -= Time.deltaTime;

        if (prepareTimer <= 0)
        {
            currentState = EnemyState.Attack;
        }
    }

    
    
    
    void Attack()
    {
        //animator.SetTrigger("isThrowing");
        // 실제 곡선 투사체는 여기서 Instantiate
        if (player != null)
        {
            GameObject soju = Instantiate(sojuPrefab, transform.position, Quaternion.identity);
            soju.GetComponent<SojuProjectile>().Launch(player.position);
        }
        currentState = EnemyState.Reset;
    }

    void ResetAfterAttack()
    {
        //animator.SetBool("isPreparing", false);
        if (player == null)
        {
            currentState = EnemyState.Patrol;
        }
        else
        {
            currentState = EnemyState.MoveToRange;
        }
    }

    
    
    
    public void OnHit()
    {
        // Removed assignment to unused isHit field
        //animator.SetTrigger("isHit");
        if (currentState == EnemyState.AttackPrepare)
        {
            currentState = EnemyState.MoveToRange;
            //animator.SetTrigger("resetAttack");
        }
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
}

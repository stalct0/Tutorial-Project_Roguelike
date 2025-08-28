using System;
using Unity.VisualScripting;
using UnityEngine;


// 적 AI 상태 정의 
public enum EnemyState {
    Patrol,
    Agro,
    MoveToRange,
    AttackPrepare,
    Attack,
    Reset,
    Stun
}


// 적 AI 정의 
public class SojuThrowerAI : MonoBehaviour
{
    private EnemyCombat combat;
    
    private float frontCheckOffset = 0.3f;   // 몸 중심에서 살짝 앞
    private float frontCheckDist   = 0.1f;   // 전방 레이 길이
    private float sameLevelYTolerance = 0.6f;// 높이 차 허용(같은 층만 충돌로 간주)
    
    public LayerMask enemyLayerMask;        // "Enemy" 레이어만 포함
    
    [SerializeField] LayerMask groundLayer = 0;    // Ground 레이어 지정
    [SerializeField] float   groundCheckDist = 0.2f;
    private Collider2D col;

    // 플레이어가 사거리를 넘어가면 몇초 후 패트롤로 돌아감. 
    private float outOfRangeTimer = 0f;
    private float outOfRangeDuration = 1f; // seconds to wait before returning to patrol



    // 패드롤 논리 변수 정의 
    private float patrolEdgeOffset = 0.3f; // Distance from edge to stop
    public EnemyState currentState = EnemyState.Patrol;


    // 플레이어 변수 정의 (사실 unity inspector 에서 할당된 값이 우선 적용됨. )
    public Transform player;
    
    private float moveSpeed = 1f;
    private float agroRange = 4f;
    private float attackRange = 5f;
    private float prepareTime = 1f;

    private float currentDirection = 1f;

    private Animator animator;
    private SpriteRenderer sr;
    
    private Rigidbody2D rb;
    // Removed unused isFacingRight field
    // Removed unused isHit field
    private float prepareTimer;
    
    private bool isFirstAttack = true;

    
    // 던지는 소주병  불러오기
    public GameObject sojuPrefab; // Assign this in the Unity Inspector
    
    static readonly int HashThrow = Animator.StringToHash("Throw");
    
    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.transform;
        }
        animator = GetComponent<Animator>();          // ★ 추가
        sr = GetComponent<SpriteRenderer>();          // ★ 추가
        rb = GetComponent<Rigidbody2D>();
        
        combat = GetComponent<EnemyCombat>(); 
        // Freeze rotation so enemy doesn't rotate
        rb.freezeRotation = true;
        
        col = GetComponent<Collider2D>();

    }
    void OnEnable()
    {
        // (선택) 이벤트에 구독해서 디버그/연출 가능
        if (combat != null)
        {
            //combat.OnStunBegin.AddListener(() => { /* 필요시 애니/이펙트 */ });
            //combat.OnLaunchedBegin.AddListener(() => { /* 필요시 애니/이펙트 */ });
            //combat.OnDied.AddListener(() => { /* 파편/코인 드롭 등 */ });
        }
    }
    
    void Update()
    {
        bool stunned = (combat != null && (combat.IsStunned || combat.IsLaunched));
        
        if (animator) animator.SetBool("isStunned", stunned);

        if (stunned)
        {
            currentState = EnemyState.Stun;
        }
        
        if (animator) animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        UpdateFacing();

        
        
        
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
            case EnemyState.Stun:
                Stun();
                break;
        }
    }
    void UpdateFacing()
    {
        float faceDir = currentDirection;
        if (player != null && (currentState == EnemyState.MoveToRange || currentState == EnemyState.AttackPrepare || currentState == EnemyState.Attack))
        {
            faceDir = Mathf.Sign(player.position.x - transform.position.x);
            if (faceDir == 0) faceDir = currentDirection;
        }
        currentDirection = Mathf.Sign(faceDir);
        if (sr) sr.flipX = (currentDirection > 0f);
        
    }

    //패트롤 
    void Patrol()
    {
        //animator.SetBool("isWalking", true);
        isFirstAttack = true;
        if (!IsGrounded())
        {
           // rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
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
                currentState = EnemyState.Agro;
                return;
            }
        }

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
    
    void Stun()
    {
        if (combat != null && !(combat.IsStunned || combat.IsLaunched))
        {
            if (animator) animator.SetBool("isStunned", false);
            currentState = EnemyState.Patrol;
        } 
    }

    void Agro()
    {
        // 어그로 끌리고 잠시 정지
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        Invoke(nameof(GoToMoveState), 0f);
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
        
        if (!IsGrounded())
        {
        //    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
        if (animator) animator.SetBool("IsPreparing", true);
        
        if (isFirstAttack)
        {
            isFirstAttack = false;
            prepareTimer = 0f; // 또는 아래에서 바로 Attack 상태로
            currentState = EnemyState.Attack;
        }
        else
        {
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
        
    }
    
    
    void Attack()
    {
        // 1) 여기서는 애니메이션만 시작
        animator.SetTrigger(HashThrow);
        currentState = EnemyState.Reset;
    }
    
    public void ThrowSoju()
    {
        if (player != null && sojuPrefab != null)
        {
            var soju = Instantiate(sojuPrefab, transform.position, Quaternion.identity);
            soju.GetComponent<SojuProjectile>().Launch(player.position);
        }
        
        currentState = (player == null) ? EnemyState.Patrol : EnemyState.MoveToRange;
    }

    void ResetAfterAttack()
    {
        if (animator) animator.SetBool("IsPreparing", false);
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
    bool IsGrounded()
    {
        if (!col) return false;
        var b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.02f);
        // 바운즈 폭만큼 아래로 BoxCast
        RaycastHit2D hit = Physics2D.BoxCast(origin, new Vector2(b.size.x * 0.9f, 0.05f), 0f, Vector2.down, groundCheckDist, groundLayer);
        Debug.DrawRay(origin, Vector2.down * groundCheckDist, Color.green, 0.05f);
        return hit.collider != null;
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

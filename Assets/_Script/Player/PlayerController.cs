using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // 점프랑 움직임
    [NonSerialized] public float moveSpeed = 5f;
    [NonSerialized] public float jumpForce = 4f;         // 기본 점프력(바로 부여)
    [NonSerialized] public float jumpHoldForce = 30f;     // 누르는 동안 프레임마다 추가 부여할 힘
    [NonSerialized] public float jumpHoldDuration = 0.10f; // 추가 힘을 줄 수 있는 최대 시간

    private bool isJumping = false;      // 점프 중인지
    private float jumpTime = 0f;         // 점프 버튼 누른 시간
    private bool jumpButtonHeld = false; // 버튼 누르고 있는지
    
    public LayerMask groundLayer;
    
    public Transform groundCheckLeft;
    public Transform groundCheckCenter;
    public Transform groundCheckRight;
    [ReadOnly] public float groundCheckDistance = 0.1f;

    private Rigidbody2D rb;
    Collider2D playerCol;
    
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool jumpRequested;
    
    private Animator animator;
    private SpriteRenderer sr;
    
    [NonSerialized] public float coyoteTime = 0.08f; // 코요테 타임
    private float coyoteTimer = 0f;

    [NonSerialized] public float jumpBufferTime = 0.12f; // 점프 버퍼
    private float jumpBufferTimer = -999f;

    [NonSerialized] public float gravityUpScale   = 1.6f; // 상승 중
    [NonSerialized] public float gravityDownScale = 1.2f; // 하강 중
    [NonSerialized] public float gravityCutScale  = 3.8f; // 점프 컷(버튼 뗐을 때)
    [NonSerialized] public float maxFallSpeed     = -21f; // 최대 낙하 속도

    [NonSerialized] public float apexThreshold = 2.0f; // |vy|가 이하면 정점 근처
    [NonSerialized] public float apexBonus     = 0.2f; // 정점 근처 이동 보너스
    
    [NonSerialized] public float apexExtraGravity = 3f; // 정점 근처 추가 중력(가중치)
    [NonSerialized] public float apexVyThreshold  = 1.2f; // |vy| 이하면 정점 근처로 간주
    
    
    //스탯 스크립트
    private PlayerStats stats;
    
    //사다리 관련
    [ReadOnly] public Tilemap ladderTilemap;
    [ReadOnly] public float ladderSpeed = 2f;
    [ReadOnly] public float snapToLadderXSpeed = 20f;
    [ReadOnly] public float ladderSnapOffset = 0f; // X 중심 보정

    private bool isOnLadder = false;
    private Vector3Int currentLadderCell;
    private float originalGravity;

    [ReadOnly] public float ladderGrabCooldown = 0.3f; // 쿨타임(초)
    [ReadOnly] public float nextLadderGrabTime = 0f;  // 다음 진입 허용 시각
    
    [ReadOnly] public float playerHeight = 0.55f;
    [ReadOnly] public float footOffset = 0.01f;
    [ReadOnly] public float headOffset = 0.01f;
    
    //근접데미지 받기 관련
    public LayerMask enemyLayer; // "Enemy"만 포함
    
    //일방향 발판 관련
    [ReadOnly] public float platformDropCooldown = 0.5f;
    [ReadOnly] public float lastPlatformDropTime = -10f;

    //스턴 관련
    private bool isShortStunned = false;
    private float shortStunTimer = 0f;
    [ReadOnly] public float shortStunDuration = 0.3f;
    [ReadOnly] public float shortStunInvincibleDuration = 0.8f;

    private bool isLongStunned = false;
    private float longStunTimer = 0f;
    [ReadOnly] public float longStunDuration = 2.0f;
    
    // Dash 관련 
    [NonSerialized] public float dashSpeed = 12f;       // 대시 속도(수평)
    [NonSerialized] public float dashDuration = 0.15f;  // 대시 유지 시간
    [NonSerialized] public float dashCooldown = 2.0f;   // 쿨타임 (요청: 2초)
    private bool  isDashing = false;               // 대시 중 여부
    private float lastDashTime = -999f;            // 마지막 대시 시각
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCol = GetComponent<Collider2D>();
        stats = GetComponent<PlayerStats>();
        stats.onDie.AddListener(OnDeath);
        
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();   
        
        originalGravity = rb.gravityScale;
        
        inputActions = new PlayerInputActions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.started += ctx => OnJumpStarted();
        inputActions.Player.Jump.canceled += ctx => OnJumpReleased();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        if (sr) sr.flipX = true;

        // ★ NEW: 코요테/버퍼 타이머 갱신(가능한 한 매 프레임 초반에)
        UpdateCoyoteAndBuffers();

        //근접 피격 적용
        CheckEnemyContactAndTakeDamage(10, 3);
        
        // ★ 대시 입력
        if (!isDashing 
            && Time.time - lastDashTime >= dashCooldown
            && !isOnLadder
            && !isLongStunned && !isShortStunned)
        {
            if ((Keyboard.current != null) &&
                (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame))
            {
                StartCoroutine(DashRoutine());
            }
        }
        
        // 스턴 처리
        if (isLongStunned)
        {
            longStunTimer -= Time.deltaTime;
            if (longStunTimer <= 0f) isLongStunned = false;

            // ★ 스턴 중에는 중력만 적용(떨어지게 하고 싶으면 아래 한 줄 유지)
            ApplyBetterGravity(); // ★ NEW
            UpdateAnimatorParams();
            return;
        }
        
        // ★ 대시 중이면 이동/점프 로직 무시, 중력은 적용할지 선택
        if (isDashing)
        {
            UpdateAnimatorParams();
            return;
        }
        
        if (isOnLadder)
        {
            HandleLadder();
            UpdateAnimatorParams();
            if (isShortStunned)
            {
                shortStunTimer -= Time.deltaTime;
                if (shortStunTimer <= 0f) isShortStunned = false;
                UpdateAnimatorParams();
                return;
            }
            // 사다리는 중력 0으로 유지 → ApplyBetterGravity 호출하지 않음
        }
        else
        {
            if (isShortStunned)
            {
                shortStunTimer -= Time.deltaTime;
                if (shortStunTimer <= 0f) isShortStunned = false;

                // 짧은 스턴 중에도 중력은 적용
                ApplyBetterGravity(); // ★ NEW
                UpdateAnimatorParams();
                return;
            }

            HandleMovement(); 
            HandleJump();     // ★ 버퍼/코요테 사용
            CheckLadderEnter();
            
            if (CanDropFromPlatform())
            {
                DropFromPlatform();
                lastPlatformDropTime = Time.time;
            }

            // ★ NEW: 공중 중력 보정
            ApplyBetterGravity();
        }

        UpdateAnimatorParams();
    }
    
    //좌우 움직임 (정점 근처 이동 보너스 추가)
    void HandleMovement()
    {
        float bonus = 0f;
        if (!IsGrounded() && Mathf.Abs(rb.linearVelocity.y) < apexThreshold)
            bonus = apexBonus; // ★ NEW

        float targetX = moveInput.x * moveSpeed * (1f + bonus); // ★ NEW
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);

        // 좌우 반전
        if (moveInput.x > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);   // x를 양수로 (오른쪽)
            transform.localScale = scale;
        }
        else if (moveInput.x < -0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);  // x를 음수로 (왼쪽)
            transform.localScale = scale;
        }
    }
    
    //점프 입력(버퍼 저장으로 변경)
    void OnJumpStarted()
    {
        if (IsOnOneWayPlatform() && moveInput.y < -0.1f)
        {
            jumpButtonHeld = true;
            return;
        }
        if (isOnLadder)
        {
            jumpRequested = true; // 사다리에서는 무조건 jumpRequested
            jumpButtonHeld = true;
            return;
        }

        // ★ 점프 버퍼에 입력만 기록
        jumpBufferTimer = jumpBufferTime; // ★ NEW
        jumpButtonHeld = true;            // ★ NEW
    }

    void OnJumpReleased()
    {
        jumpButtonHeld = false;
    }

    // 점프 처리(코요테+버퍼+가변 점프)
    void HandleJump()
    {
        // ★ 1) 점프 시작 조건: 버퍼 + 코요테
        if (jumpBufferTimer > 0f)
        {
            bool canStartJump = (IsGrounded() || coyoteTimer > 0f) && !isOnLadder;
            if (canStartJump)
            {
                jumpBufferTimer = -999f; // 버퍼 소모
                coyoteTimer = 0f;

                isJumping = true;
                jumpTime = 0f;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        // ★ 2) 가변 점프 유지
        if (isJumping)
        {
            if (jumpButtonHeld && jumpTime < jumpHoldDuration)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y + jumpHoldForce * Time.deltaTime
                );
                jumpTime += Time.deltaTime;
            }
            else
            {
                isJumping = false; // 더이상 추가 힘 안 줌
            }

            // 혹시 땅에 다시 닿으면 isJumping 해제
            if (IsGrounded() && rb.linearVelocity.y <= 0f)
            {
                isJumping = false;
            }
        }
    }

    // ★ NEW: 코요테/버퍼 타이머 업데이트
    void UpdateCoyoteAndBuffers()
    {
        // 코요테: 땅이면 리필, 아니면 감소
        if (IsGrounded()) coyoteTimer = coyoteTime;
        else              coyoteTimer -= Time.deltaTime;

        // 점프 버퍼 감소
        jumpBufferTimer -= Time.deltaTime;
    }

    // ★ NEW: 점프 컷/중력 가중치/최대 낙하 속도
    void ApplyBetterGravity()
    {
        if (isOnLadder) return; // 사다리는 중력 0 유지

        float g = originalGravity;

        if (rb.linearVelocity.y > 0.01f)
        {
            // 상승 중: 버튼 유지면 upScale, 떼면 cutScale
            g *= (jumpButtonHeld ? gravityUpScale : gravityCutScale);
        }
        else if (rb.linearVelocity.y < -0.01f)
        {
            // 하강 중
            g *= gravityDownScale;
        }
        
        // ★ 정점 보정: 거의 0에 가까운 순간 약간 더 눌러서 '붕 뜸' 제거
        if (Mathf.Abs(rb.linearVelocity.y) < apexVyThreshold)
            g += apexExtraGravity;
        
        rb.gravityScale = g;

        // 최대 낙하 속도 제한
        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    bool IsGrounded()
    {
        return RayHit(groundCheckLeft) || RayHit(groundCheckCenter) || RayHit(groundCheckRight);
    }

    bool RayHit(Transform origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin.position, Vector2.down, groundCheckDistance, groundLayer);
    #if UNITY_EDITOR
        Debug.DrawRay(origin.position, Vector2.down * groundCheckDistance, Color.red);
    #endif
        return hit.collider != null;
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;

        // 바라보는 방향으로 수평 대시 (localScale.x의 부호 사용)
        float dirX = Mathf.Sign(transform.localScale.x);
        // ★ 대시 시작 순간, 수직 속도 0으로 초기화
        rb.linearVelocity = new Vector2(dirX * dashSpeed, 0f);

        // 대시 지속 시간 동안 유지 (이 동안 이동/점프 로직은 Update에서 return으로 정지됨)
        float t = 0f;
        while (t < dashDuration)
        {
            // 외부에서 X속도를 덮어쓰지 않도록 매 프레임 유지(원치 않으면 이 줄은 생략 가능)
            rb.linearVelocity = new Vector2(dirX * dashSpeed, 0f);
            t += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        // 대시 종료 후 추가 감속/처리는 취향에 따라
    }
    
    //몬스터 근접공격 받기
    public void CheckEnemyContactAndTakeDamage(int damage = 10, float knockbackForce = 3)
    {
        Vector2 checkCenter = transform.position;
        Vector2 checkSize = new Vector2(0.4f, 0.55f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, enemyLayer);

        foreach (var hit in hits)
        {
            stats.TakeDamageKnockback(damage, hit.transform.position, knockbackForce);
            break; // 여러 몬스터와 겹쳐도 1회만
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireCube(transform.position,new Vector2(0.4f, 0.55f));
    }
    
    //스턴
    public void LongStun(float duration)
    {
        isLongStunned = true;
        longStunTimer = duration;
        rb.linearVelocity = Vector2.zero; // 즉시 멈춤 (선택)
    }
    public void ShortStun(float stunDuration)
    {
        isShortStunned = true;
        shortStunTimer = stunDuration;
        rb.linearVelocity = Vector2.zero; // 즉시 멈춤 (선택)
    }
    

    //넉백
    public void KnockbackFrom(Vector2 sourcePosition, float knockbackForce)
    {
        Vector2 knockbackDir = ((Vector2)transform.position - sourcePosition).normalized;
        knockbackDir.y = 0.5f; 
        knockbackDir.Normalize();
        
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
    }
    
    void OnDeath()
    {
        //죽는 애니
    }
    
    
    //사다리
    void CheckLadderEnter()
    {
        if (Time.time < nextLadderGrabTime)
            return;
        
        Vector3Int cellCenter = ladderTilemap.WorldToCell(transform.position);
        Vector3Int cellFeet = ladderTilemap.WorldToCell(transform.position + Vector3.down * 0.5f);

        bool ladderHere = ladderTilemap.GetTile(cellCenter) != null || ladderTilemap.GetTile(cellFeet) != null;
        bool climbingInput = Mathf.Abs(moveInput.y) > 0.1f;

        if (IsOnLadderArea() && climbingInput)
        {
            isOnLadder = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            currentLadderCell = cellCenter;
        }
    }
    
    bool IsOnLadderArea()
    {
        Vector3 center = transform.position;
        Vector3 feet = center + Vector3.down * (playerHeight * 0.5f - footOffset);
        Vector3 head = center + Vector3.up * (playerHeight * 0.5f - headOffset);

        Vector3Int cellCenter = ladderTilemap.WorldToCell(center);
        Vector3Int cellFeet = ladderTilemap.WorldToCell(feet);
        Vector3Int cellHead = ladderTilemap.WorldToCell(head);

        return
            ladderTilemap.GetTile(cellCenter) != null ||
            ladderTilemap.GetTile(cellFeet) != null ||
            ladderTilemap.GetTile(cellHead) != null;
    }


    void HandleLadder() // 사다리 타고 있는 상태에서는 이렇게 처리
    {
        // 좌우 반전
        if (moveInput.x > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);   // x를 양수로 (오른쪽)
            transform.localScale = scale;
        }
        else if (moveInput.x < -0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);  // x를 음수로 (왼쪽)
            transform.localScale = scale;
        }
        
        Vector3 playerPos = transform.position;
        // X좌표를 사다리 중앙에 스냅
        float targetX = ladderTilemap.CellToWorld(currentLadderCell).x + ladderTilemap.cellSize.x * 0.5f +
                        ladderSnapOffset;
        rb.position = new Vector2(
            Mathf.Lerp(rb.position.x, targetX, Time.deltaTime * snapToLadderXSpeed),
            rb.position.y
        );

        float verticalMove = moveInput.y;
        
        // 사다리 상태에서 아래키만 누르면 일방향 발판 통과
        if (moveInput.y < -0.1f && IsOnOneWayPlatform() && Time.time - lastPlatformDropTime >= platformDropCooldown)
        {
            DropFromPlatform();
            lastPlatformDropTime = Time.time;
        }
        
        Vector2 velocity = rb.linearVelocity;

        if (verticalMove > 0 && IsOnLadderCanMoveUp())
        {
            velocity.y = verticalMove * ladderSpeed;
        }
        else if (verticalMove < 0 && IsOnLadderCanMoveDown())
        {
            velocity.y = verticalMove * ladderSpeed;
        }
        else{
            velocity.y = 0f;
        }
        
        velocity.x = 0f; // 사다리 중엔 좌우 이동 불가
        rb.linearVelocity = velocity;

        // 점프 입력시 사다리 탈출
        if (jumpRequested)
        {
            if (moveInput.y < -0.1f)
            {
                // 아래 방향키와 함께 점프: 점프하지 않고 단순 탈출만
                ExitLadder();
                jumpRequested = false;
                return;
            }
            else
            {
                // 위/중앙 + 점프 탈출
                ExitLadder();
                isJumping = true;
                jumpButtonHeld = true;
                jumpTime = 0f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpRequested = false;
                return;
            }
        }

        Vector3Int cellCenter = ladderTilemap.WorldToCell(transform.position);
        if (ladderTilemap.GetTile(cellCenter) == null)
        {
            // 단, 의도적으로 점프를 눌러 탈출한 게 아니면
            // ExitLadder();
        }
    }

    void ExitLadder()
    {
        isOnLadder = false;
        rb.gravityScale = originalGravity;
        nextLadderGrabTime = Time.time + ladderGrabCooldown;

    }
    bool IsOnLadderCanMoveDown()
    {
        Vector3 center = transform.position;
        Vector3 feet = center + Vector3.down * (playerHeight * 0.5f - footOffset);

        Vector3Int cellCenter = ladderTilemap.WorldToCell(center);
        Vector3Int cellFeet = ladderTilemap.WorldToCell(feet);

        return
            ladderTilemap.GetTile(cellCenter) != null ||
            ladderTilemap.GetTile(cellFeet) != null;
    }
    bool IsOnLadderCanMoveUp()
    {
        Vector3 center = transform.position;
        Vector3 head = center + Vector3.up * (playerHeight * 0.5f - headOffset);

        Vector3Int cellCenter = ladderTilemap.WorldToCell(center);
        Vector3Int cellHead = ladderTilemap.WorldToCell(head);

        return
            ladderTilemap.GetTile(cellCenter) != null ||
            ladderTilemap.GetTile(cellHead) != null;
    }
    
    
    //일방향 발판 
    void DropFromPlatform()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * playerHeight/2;
        foreach (var col in Physics2D.OverlapBoxAll(origin, playerCol.bounds.size * new Vector2(0.9f, 0.1f), 0))
        {
            if (col.CompareTag("OneWayPlatform"))
            {
                Physics2D.IgnoreCollision(playerCol, col, true);
                StartCoroutine(RestorePlatformCollision(col, platformDropCooldown));
            }
        }
    }

    IEnumerator RestorePlatformCollision(Collider2D platformCol, float delay)
    {
        yield return new WaitForSeconds(delay);
        Physics2D.IgnoreCollision(playerCol, platformCol, false);
    }
    
    bool CanDropFromPlatform()
    {
        // 쿨타임 중엔 드롭 불가
        if (Time.time - lastPlatformDropTime < platformDropCooldown)
        {
            return false;
        }

        if (moveInput.y < -0.1f && jumpButtonHeld && IsOnOneWayPlatform())
        {
            return true;
        }

        return false;
    }
    bool IsOnOneWayPlatform()
    {
        // 아래로 약간 오프셋(플레이어 발 바로 아래)만 체크해도 됨
        Vector2 origin = (Vector2)transform.position + Vector2.down * playerHeight/2;
        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, playerCol.bounds.size * new Vector2(0.9f, 0.1f), 0);

        foreach (var col in hits)
        {
            if (col != playerCol && col.CompareTag("OneWayPlatform"))
            {
                return true;
            }
        }
        return false;
    }
    
    void UpdateAnimatorParams()
    {
        if (!animator) return;

        bool stunned = isLongStunned || isShortStunned;
        animator.SetBool("IsStunned", stunned);

        bool grounded = IsGrounded();
        animator.SetBool("IsGrounded", grounded);

        animator.SetBool("IsClimbing", isOnLadder);

        // Speed: 사다리/대시/스턴 중이면 0으로 고정(원하는 감각에 맞춰 조정)
        float speed = (isOnLadder || isDashing || stunned) ? 0f : Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", speed);

        // 선택: 공중 블렌드용
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }
}
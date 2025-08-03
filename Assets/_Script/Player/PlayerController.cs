using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // 점프랑 움직임
    [NonSerialized] public float moveSpeed = 5f;
    
    public float jumpHoldForce = 8f;   // 누르는 동안 추가 힘
    public float jumpHoldDuration = 0.15f; // 최대 추가 시간(초)

    private bool isJumping = false;         // 점프 중인지
    private float jumpTime = 0f;            // 점프 버튼을 누르고 있는 시간
    private bool jumpButtonHeld = false;    // 버튼이 계속 눌려있는지
    
    [NonSerialized] public float jumpForce = 7f;
    public LayerMask groundLayer;
    
    public Transform groundCheckLeft;
    public Transform groundCheckCenter;
    public Transform groundCheckRight;
    public float groundCheckDistance = 0.1f;

    private Rigidbody2D rb;
    
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool jumpRequested;
    
    //스탯 스크립트
    private PlayerStats stats;
    
    //사다리 관련
    public Tilemap ladderTilemap;
    public float ladderSpeed = 2f;
    public float snapToLadderXSpeed = 20f;
    public float ladderSnapOffset = 0f; // X 중심 보정

    private bool isOnLadder = false;
    private Vector3Int currentLadderCell;
    private float originalGravity;

    private float ladderGrabCooldown = 0.3f; // 쿨타임(초)
    private float nextLadderGrabTime = 0f;  // 다음 진입 허용 시각
    
    private float playerHeight = 0.55f;
    private float footOffset = 0.01f;
    private float headOffset = 0.01f;
    
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        stats.onDie.AddListener(OnDeath);
        
        originalGravity = rb.gravityScale;
        
        inputActions = new PlayerInputActions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += ctx => jumpRequested = true;
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
        Debug.Log(Time.time <  nextLadderGrabTime);
        
        if (isOnLadder)
            HandleLadder();
        else
        {
            HandleMovement();
            HandleJump();
            CheckLadderEnter();
        }
        
    }

    void HandleMovement()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    void HandleJump()
    {
        if (jumpRequested && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        jumpRequested = false;
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

    void OnDeath()
    {
        //죽는 애니
    }
    
    void CheckLadderEnter()
    {
        if (Time.time < nextLadderGrabTime)
            return;
        
        // 아래에서 중심, 여러 위치에서 ladderTilemap 겹침 체크
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
        // 플레이어 중심 좌표 기준
        Vector3 center = transform.position;
        // 플레이어 발 위치 기준 (Collider 크기 맞게 조정)
        Vector3 feet = center + Vector3.down * (playerHeight * 0.5f - footOffset);
        // 플레이어 머리 위치 (필요하면)
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
        Vector3 playerPos = transform.position;
        // X좌표를 사다리 중앙에 스냅
        float targetX = ladderTilemap.CellToWorld(currentLadderCell).x + ladderTilemap.cellSize.x * 0.5f +
                        ladderSnapOffset;
        rb.position = new Vector2(
            Mathf.Lerp(rb.position.x, targetX, Time.deltaTime * snapToLadderXSpeed),
            rb.position.y
        );

        float verticalMove = moveInput.y;
        
        
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

        
        
        velocity.x = 0f; // 사다리 중엔 X 이동 강제 0
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
                // 위/중앙 + 점프: 탈출 + 점프 수행
                ExitLadder();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpRequested = false;
                return;
            }
        }

        Vector3Int cellCenter = ladderTilemap.WorldToCell(transform.position);
        if (ladderTilemap.GetTile(cellCenter) == null)
        {
            // 단, 의도적으로 점프를 눌러 탈출한 게 아니면 X
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
        Vector3 feet = center + Vector3.down * (playerHeight * 0.4f - footOffset);

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
    
}
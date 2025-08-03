using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [NonSerialized] public float moveSpeed = 5f;
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

    private PlayerStats stats;
    
    //사다리 관련
    public Tilemap ladderTilemap;
    public float ladderSpeed = 2f;
    public float snapToLadderXSpeed = 20f;
    public float ladderSnapOffset = 0f; // X 중심 보정

    private bool isOnLadder = false;
    private Vector3Int currentLadderCell;
    private float originalGravity;

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

    void HandleLadder()
    {
        Vector3 playerPos = transform.position;
        // X좌표를 사다리 중앙에 스냅
        float targetX = ladderTilemap.CellToWorld(currentLadderCell).x + ladderTilemap.cellSize.x * 0.5f +
                        ladderSnapOffset;
        rb.position = new Vector2(
            Mathf.Lerp(rb.position.x, targetX, Time.deltaTime * snapToLadderXSpeed),
            rb.position.y
        );

        int verticalInt = 0;
        if (moveInput.y > 0.1f)
            verticalInt = 1;
        else if (moveInput.y < -0.1f)
            verticalInt = -1;
        else
            verticalInt = 0;
        
        Vector2 velocity = rb.linearVelocity;

        if (verticalInt > 0 && IsOnLadderCanMoveUp())
        {
            velocity.y = verticalInt * ladderSpeed;
        }
        else if (verticalInt < 0 && IsOnLadderCanMoveDown())
        {
            velocity.y = verticalInt * ladderSpeed;
        }
        else{
            velocity.y = 0f;
        }

        
        
        velocity.x = 0f; // 사다리 중엔 X 이동 강제 0
        rb.linearVelocity = velocity;

        // 점프 입력시 사다리 탈출
        if (jumpRequested)
        {
            ExitLadder();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
            return;
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
    }

    
    
    
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        HandleMovement();
        HandleJump();
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
}
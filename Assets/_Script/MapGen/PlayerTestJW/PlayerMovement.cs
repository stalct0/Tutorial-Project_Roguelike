using UnityEngine;
using UnityEngine.InputSystem;


public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    void Update()
    {
        float moveInput = 0f;
        if (Keyboard.current.aKey.isPressed) moveInput -= 1f;
        if (Keyboard.current.dKey.isPressed) moveInput += 1f;
        transform.position += new Vector3(moveInput, 0, 0) * moveSpeed * Time.deltaTime;
    }
}

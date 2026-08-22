using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpHoldForce = 15f;
    public float maxJumpTime = 0.4f;

    [Header("Gravity")]
    public float gravityMultiplier = 2f;

    private Rigidbody rb;
    private Vector3 movement;

    private bool isGrounded;
    private bool isJumping;
    private float jumpTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal = -1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal = 1f;

        if (Keyboard.current.wKey.isPressed)
            vertical = 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical = -1f;

        // Movimiento relativo a la rotación del personaje
        movement =
            transform.right * horizontal +
            transform.forward * vertical;

        movement.Normalize();

        // Comenzar salto
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            isGrounded = false;
            isJumping = true;
            jumpTime = 0f;
        }

        // Soltar espacio
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            isJumping = false;
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;

        // Movimiento horizontal
        velocity.x = movement.x * speed;
        velocity.z = movement.z * speed;

        rb.linearVelocity = velocity;

        // Salto variable
        if (isJumping && Keyboard.current.spaceKey.isPressed)
        {
            if (jumpTime < maxJumpTime)
            {
                rb.AddForce(
                    Vector3.up * jumpHoldForce,
                    ForceMode.Acceleration
                );

                jumpTime += Time.fixedDeltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        // Gravedad
        if (!isGrounded)
        {
            rb.AddForce(
                Physics.gravity * (gravityMultiplier - 1f),
                ForceMode.Acceleration
            );
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            isJumping = false;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
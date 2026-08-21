using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 7f;

    [Header("Rotation")]
    public float rotationSpeed = 180f;

    [Header("Air Control")]
    public float airControl = 0.25f;

    [Header("Gravity")]
    public float gravityMultiplier = 2f;

    private Rigidbody rb;
    private Vector3 movement;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            horizontal = -1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            horizontal = 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            vertical = -1f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            vertical = 1f;

        if (isGrounded)
        {
            // A/D rotan al personaje
            if (horizontal != 0f)
            {
                transform.Rotate(
                    Vector3.up * horizontal * rotationSpeed * Time.deltaTime
                );
            }

            // W/S mueven al personaje según su frente
            movement = transform.forward * vertical;
        }
        else
        {
            // En el aire A/D mueven lateralmente
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            movement =
                (forward * vertical) +
                (right * horizontal);

            movement.Normalize();
        }

        // Salto
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;

        if (isGrounded)
        {
            // Movimiento normal en el suelo
            velocity.x = movement.x * speed;
            velocity.z = movement.z * speed;
        }
        else
        {
            // Control reducido en el aire
            velocity.x += movement.x * speed * airControl * Time.fixedDeltaTime;
            velocity.z += movement.z * speed * airControl * Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity;

        // Gravedad adicional
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
        }
    }
}
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

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckHeight = 1f;
    public float groundCheckRadius = 0.25f;
    public float groundCheckDistance = 0.5f;

    private Rigidbody rb;
    private Vector3 movement;

    private bool isGrounded;

    // Salto variable
    private bool isJumping;
    private float jumpTime;

    // Normal de la superficie vertical que estamos tocando
    private Vector3 wallNormal;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    void Update()
    {
        // =========================
        // DETECTAR SUELO
        // =========================

        CheckGround();


        // =========================
        // INPUT DE MOVIMIENTO
        // =========================

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


        // =========================
        // MOVIMIENTO RELATIVO AL PLAYER
        // =========================

        movement =
            transform.right * horizontal +
            transform.forward * vertical;

        if (movement.magnitude > 1f)
            movement.Normalize();


        // =========================
        // COMENZAR SALTO
        // =========================

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;
            isJumping = true;
            jumpTime = 0f;
        }


        // =========================
        // SOLTAR ESPACIO
        // =========================

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            isJumping = false;
        }
    }


    void FixedUpdate()
    {
        // =========================
        // VELOCIDAD ACTUAL
        // =========================

        Vector3 velocity = rb.linearVelocity;


        // =========================
        // MOVIMIENTO DESEADO
        // =========================

        Vector3 desiredVelocity =
            movement * speed;


        // =========================
        // EVITAR EMPUJAR CONTRA PAREDES
        // =========================

        if (wallNormal != Vector3.zero)
        {
            desiredVelocity = Vector3.ProjectOnPlane(
                desiredVelocity,
                wallNormal
            );
        }


        // =========================
        // APLICAR MOVIMIENTO
        // =========================

        velocity.x = desiredVelocity.x;
        velocity.z = desiredVelocity.z;

        rb.linearVelocity = velocity;


        // =========================
        // SALTO VARIABLE
        // =========================

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
                // Llegó al tiempo máximo
                isJumping = false;
            }
        }


        // =========================
        // LIMPIAR NORMAL DE PARED
        // =========================

        wallNormal = Vector3.zero;
    }


    // =====================================================
    // DETECCIÓN DEL SUELO
    // =====================================================

    void CheckGround()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * groundCheckHeight;

        RaycastHit hit;

        bool detected = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );


        // Una superficie con normal Y alta es suelo.
        //
        // Suelo:
        // normal.y ≈ 1
        //
        // Pared:
        // normal.y ≈ 0

        if (detected && hit.normal.y > 0.5f)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }


    // =====================================================
    // DETECCIÓN DE PAREDES
    // =====================================================

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;


            // Si la normal tiene poco componente Y,
            // estamos tocando una superficie vertical.

            if (normal.y < 0.5f)
            {
                wallNormal = normal;
            }
        }
    }


    // =====================================================
    // VISUALIZACIÓN DEL SPHERECAST
    // =====================================================

    void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * groundCheckHeight;

        RaycastHit hit;

        bool detected = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );


        // =========================
        // COLOR
        // =========================

        if (detected && hit.normal.y > 0.5f)
        {
            Gizmos.color = Color.green;
        }
        else if (detected)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.red;
        }


        // =========================
        // ESFERA INICIAL
        // =========================

        Gizmos.DrawWireSphere(
            origin,
            groundCheckRadius
        );


        // =========================
        // ESFERA FINAL
        // =========================

        Vector3 end =
            origin +
            Vector3.down *
            groundCheckDistance;

        Gizmos.DrawWireSphere(
            end,
            groundCheckRadius
        );


        // =========================
        // RECORRIDO
        // =========================

        Gizmos.DrawLine(
            origin,
            end
        );
    }
}
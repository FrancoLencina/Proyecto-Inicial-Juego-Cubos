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
    private PlayerInteraction playerInteraction;

    private Vector3 movement;

    private bool isGrounded;

    private bool isJumping;
    private float jumpTime;

    private Vector3 wallNormal;

    // Rotación solicitada por la cámara.
    private float pendingRotation;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        rb =
            GetComponent<Rigidbody>();

        playerInteraction =
            GetComponent<PlayerInteraction>();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        CheckGround();


        // =====================================================
        // INPUT DE MOVIMIENTO
        // =====================================================

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


        movement =
            transform.right * horizontal +
            transform.forward * vertical;


        if (movement.magnitude > 1f)
            movement.Normalize();


        // =====================================================
        // COMENZAR SALTO
        // =====================================================

        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;
            isJumping = true;
            jumpTime = 0f;
        }


        // =====================================================
        // SOLTAR ESPACIO
        // =====================================================

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            isJumping = false;
        }
    }


    // =========================================================
    // SOLICITAR ROTACIÓN
    // =========================================================

    public void RequestRotation(float rotation)
    {
        pendingRotation += rotation;
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    void FixedUpdate()
    {
        Vector3 velocity =
            rb.linearVelocity;


        // =====================================================
        // MOVIMIENTO
        // =====================================================

        Vector3 desiredVelocity =
            movement * speed;


        Vector3 desiredMovement =
            desiredVelocity *
            Time.fixedDeltaTime;


        // =====================================================
        // BLOQUE SOSTENIDO
        // =====================================================

        if (playerInteraction != null &&
            playerInteraction.IsHoldingBlock)
        {
            desiredMovement =
                playerInteraction.GetAllowedPlayerMovement(
                    desiredMovement
                );
        }


        // =====================================================
        // CONVERTIR A VELOCIDAD
        // =====================================================

        if (Time.fixedDeltaTime > 0f)
        {
            desiredVelocity =
                desiredMovement /
                Time.fixedDeltaTime;
        }


        // =====================================================
        // EVITAR EMPUJAR CONTRA PAREDES
        // =====================================================

        if (wallNormal != Vector3.zero)
        {
            float movementIntoWall =
                Vector3.Dot(
                    desiredVelocity,
                    wallNormal
                );


            if (movementIntoWall < 0f)
            {
                desiredVelocity =
                    Vector3.ProjectOnPlane(
                        desiredVelocity,
                        wallNormal
                    );
            }
        }


        // =====================================================
        // APLICAR MOVIMIENTO
        // =====================================================

        velocity.x =
            desiredVelocity.x;

        velocity.z =
            desiredVelocity.z;


        rb.linearVelocity =
            velocity;


        // =====================================================
        // ROTACIÓN
        // =====================================================

        if (Mathf.Abs(pendingRotation) >
            0.0001f)
        {
            float requestedRotation =
                pendingRotation;

            pendingRotation = 0f;


            float allowedRotation =
                requestedRotation;


            // -------------------------------------------------
            // SI HAY BLOQUE SOSTENIDO
            // -------------------------------------------------

            if (playerInteraction != null &&
                playerInteraction.IsHoldingBlock)
            {
                Vector3 correction =
                    playerInteraction.GetRotationCorrection(
                        requestedRotation,
                        out allowedRotation
                    );


                // -------------------------------------------------
                // RETROCESO MÍNIMO NECESARIO
                // -------------------------------------------------

                if (correction.sqrMagnitude >
                    0.000001f)
                {
                    rb.MovePosition(
                        rb.position +
                        correction
                    );
                }
            }


            // -------------------------------------------------
            // APLICAR ROTACIÓN PERMITIDA
            // -------------------------------------------------

            if (Mathf.Abs(allowedRotation) >
                0.0001f)
            {
                Quaternion targetRotation =
                    rb.rotation *
                    Quaternion.Euler(
                        0f,
                        allowedRotation,
                        0f
                    );


                rb.MoveRotation(
                    targetRotation
                );
            }
        }


        // =====================================================
        // SALTO VARIABLE
        // =====================================================

        if (isJumping &&
            Keyboard.current.spaceKey.isPressed)
        {
            if (jumpTime < maxJumpTime)
            {
                rb.AddForce(
                    Vector3.up *
                    jumpHoldForce,
                    ForceMode.Acceleration
                );

                jumpTime +=
                    Time.fixedDeltaTime;
            }
            else
            {
                isJumping = false;
            }
        }


        // =====================================================
        // LIMPIAR NORMAL
        // =====================================================

        wallNormal =
            Vector3.zero;
    }


    // =========================================================
    // DETECCIÓN DEL SUELO
    // =========================================================

    void CheckGround()
    {
        Vector3 origin =
            transform.position +
            Vector3.up *
            groundCheckHeight;


        RaycastHit hit;


        bool detected =
            Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out hit,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );


        if (detected &&
            hit.normal.y > 0.5f)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }


    // =========================================================
    // DETECCIÓN DE PAREDES
    // =========================================================

    void OnCollisionStay(
        Collision collision
    )
    {
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer(
                "HeldFruitBlock"
            ))
        {
            return;
        }


        if (collision.gameObject.layer ==
            LayerMask.NameToLayer(
                "FruitBlocks"
            ))
        {
            return;
        }


        foreach (ContactPoint contact
                 in collision.contacts)
        {
            Vector3 normal =
                contact.normal;


            if (normal.y < 0.5f)
            {
                wallNormal =
                    normal;
            }
        }
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up *
            groundCheckHeight;


        RaycastHit hit;


        bool detected =
            Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out hit,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );


        if (detected &&
            hit.normal.y > 0.5f)
        {
            Gizmos.color =
                Color.green;
        }
        else if (detected)
        {
            Gizmos.color =
                Color.yellow;
        }
        else
        {
            Gizmos.color =
                Color.red;
        }


        Gizmos.DrawWireSphere(
            origin,
            groundCheckRadius
        );


        Vector3 end =
            origin +
            Vector3.down *
            groundCheckDistance;


        Gizmos.DrawWireSphere(
            end,
            groundCheckRadius
        );


        Gizmos.DrawLine(
            origin,
            end
        );
    }
}
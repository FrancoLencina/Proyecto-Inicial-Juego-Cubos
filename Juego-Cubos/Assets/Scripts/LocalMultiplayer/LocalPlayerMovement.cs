using UnityEngine;
using UnityEngine.InputSystem;

public class LocalPlayerMovement : MonoBehaviour
{
    public enum PlayerNumber
    {
        Player1,
        Player2
    }

    [Header("Player")]
    [SerializeField] private PlayerNumber playerNumber = PlayerNumber.Player1;

    // Necesario para que LocalGameManager identifique a cada jugador.
    public PlayerNumber PlayerType => playerNumber;

    [Header("Movement")]
    public float speed = 5f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.5f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpHoldForce = 15f;
    public float maxJumpTime = 0.4f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckHeight = 1f;
    public float groundCheckRadius = 0.25f;
    public float groundCheckDistance = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody rb;
    private PlayerInteraction playerInteraction;
    private PlayerControls controls;

    private float verticalInput;
    private float horizontalInput;

    private bool isSprinting;

    private bool isGrounded;
    public bool IsGrounded => isGrounded;

    private bool isJumping;
    private float jumpTime;

    private Vector3 wallNormal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerInteraction = GetComponent<PlayerInteraction>();

        controls = new PlayerControls();

        InputSettings.LoadBindings(controls);
    }

    private void OnEnable()
    {
        if (controls != null)
            controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
            controls.Disable();
    }

    private void Update()
    {
        CheckGround();

        if (animator != null)
            animator.SetBool("IsGrounded", isGrounded);

        ReadMovementInput();
        ReadSprintInput();

        UpdateAnimations();

        HandleJumpInput();
    }

    // =========================================================
    // MOVEMENT INPUT
    // =========================================================

    private void ReadMovementInput()
    {
        if (playerNumber == PlayerNumber.Player1)
            ReadPlayer1Movement();
        else
            ReadPlayer2Movement();
    }

    private void ReadPlayer1Movement()
    {
        verticalInput = 0f;
        horizontalInput = 0f;

        if (Keyboard.current == null)
            return;

        // W / S = avanzar / retroceder
        if (Keyboard.current.wKey.isPressed)
            verticalInput = 1f;
        else if (Keyboard.current.sKey.isPressed)
            verticalInput = -1f;

        // A / D = rotar
        if (Keyboard.current.aKey.isPressed)
            horizontalInput = -1f;
        else if (Keyboard.current.dKey.isPressed)
            horizontalInput = 1f;
    }

    private void ReadPlayer2Movement()
    {
        verticalInput = 0f;
        horizontalInput = 0f;

        if (Keyboard.current == null)
            return;

        // Flecha arriba / abajo = avanzar / retroceder
        if (Keyboard.current.upArrowKey.isPressed)
            verticalInput = 1f;
        else if (Keyboard.current.downArrowKey.isPressed)
            verticalInput = -1f;

        // Flecha izquierda / derecha = rotar
        if (Keyboard.current.leftArrowKey.isPressed)
            horizontalInput = -1f;
        else if (Keyboard.current.rightArrowKey.isPressed)
            horizontalInput = 1f;
    }

    // =========================================================
    // SPRINT
    // =========================================================

    private void ReadSprintInput()
    {
        if (Keyboard.current == null)
        {
            isSprinting = false;
            return;
        }

        if (playerNumber == PlayerNumber.Player1)
        {
            isSprinting =
                Keyboard.current.leftShiftKey.isPressed;
        }
        else
        {
            isSprinting =
                Keyboard.current.rightShiftKey.isPressed;
        }
    }

    // =========================================================
    // ANIMATIONS
    // =========================================================

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        animator.SetFloat("VelX", 0f);
        animator.SetFloat("VelY", verticalInput);
    }

    // =========================================================
    // JUMP
    // =========================================================

    private void HandleJumpInput()
    {
        if (!isGrounded)
            return;

        bool jumpPressed = false;

        if (Keyboard.current != null)
        {
            if (playerNumber == PlayerNumber.Player1)
            {
                // Player 1 = Space
                jumpPressed =
                    Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            else
            {
                // Player 2 = Numpad 0
                jumpPressed =
                    Keyboard.current.numpad0Key.wasPressedThisFrame;
            }
        }

        if (jumpPressed)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;

            isJumping = true;
            jumpTime = 0f;

            if (animator != null)
                animator.SetTrigger("Jump");
        }
    }

    // =========================================================
    // PHYSICS MOVEMENT
    // =========================================================

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;

        float currentSpeed =
            isSprinting
                ? speed * sprintMultiplier
                : speed;

        // El personaje siempre avanza según SU forward.
        Vector3 forward = transform.forward;

        Vector3 desiredVelocity =
            forward *
            verticalInput *
            currentSpeed;

        Vector3 desiredMovement =
            desiredVelocity *
            Time.fixedDeltaTime;

        // Si está agarrando un bloque, respetar las
        // restricciones de movimiento de PlayerInteraction.
        if (playerInteraction != null &&
            playerInteraction.IsHoldingBlock)
        {
            desiredMovement =
                playerInteraction.GetAllowedPlayerMovement(
                    desiredMovement
                );
        }

        if (Time.fixedDeltaTime > 0f)
        {
            desiredVelocity =
                desiredMovement /
                Time.fixedDeltaTime;
        }

        // =====================================================
        // WALL SLIDING
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

        velocity.x = desiredVelocity.x;
        velocity.z = desiredVelocity.z;

        if (isGrounded && !isJumping)
            velocity.y = 0f;

        rb.linearVelocity = velocity;

        // =====================================================
        // ROTATION
        // =====================================================

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            float rotationAmount =
                horizontalInput *
                rotationSpeed *
                Time.fixedDeltaTime;

            Quaternion rotation =
                rb.rotation *
                Quaternion.Euler(
                    0f,
                    rotationAmount,
                    0f
                );

            // Si está agarrando un bloque, comprobar
            // cuánto puede rotar sin atravesarlo.
            if (playerInteraction != null &&
                playerInteraction.IsHoldingBlock)
            {
                float requestedRotation =
                    Mathf.Abs(rotationAmount);

                Vector3 correction =
                    playerInteraction.GetRotationCorrection(
                        requestedRotation,
                        out float allowedRotation
                    );

                if (correction.sqrMagnitude > 0.000001f)
                {
                    rb.MovePosition(
                        rb.position +
                        correction
                    );
                }

                float direction =
                    Mathf.Sign(horizontalInput);

                rotation =
                    rb.rotation *
                    Quaternion.Euler(
                        0f,
                        allowedRotation * direction,
                        0f
                    );
            }

            rb.MoveRotation(rotation);
        }

        // =====================================================
        // VARIABLE JUMP
        // =====================================================

        if (isJumping)
        {
            bool jumpHeld = false;

            if (Keyboard.current != null)
            {
                if (playerNumber == PlayerNumber.Player1)
                {
                    jumpHeld =
                        Keyboard.current.spaceKey.isPressed;
                }
                else
                {
                    jumpHeld =
                        Keyboard.current.numpad0Key.isPressed;
                }
            }

            if (jumpHeld)
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
            else
            {
                isJumping = false;
            }
        }

        wallNormal = Vector3.zero;
    }

    // =========================================================
    // GROUND CHECK
    // =========================================================

    private void CheckGround()
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

        if (detected && hit.normal.y > 0.5f)
            isGrounded = true;
        else
            isGrounded = false;
    }

    // =========================================================
    // COLLISIONS
    // =========================================================

    private void OnCollisionStay(Collision collision)
    {
        // Ignorar bloques que estamos agarrando.
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("HeldFruitBlock"))
            return;

        // Ignorar bloques de fruta.
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("FruitBlocks"))
            return;

        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal =
                contact.normal;

            // Detectar paredes.
            if (normal.y < 0.5f)
            {
                wallNormal = normal;
            }
        }
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
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

        if (detected && hit.normal.y > 0.5f)
            Gizmos.color = Color.green;
        else if (detected)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.red;

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
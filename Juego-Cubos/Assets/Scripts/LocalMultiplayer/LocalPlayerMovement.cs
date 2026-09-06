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

    [Header("DEBUG")]
    [SerializeField] private bool enableDebugLogs = true;

    // Cada cuánto mostrar el estado físico.
    [SerializeField] private float physicsLogInterval = 0.15f;

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

    // =========================================================
    // DEBUG VARIABLES
    // =========================================================

    private bool previousGrounded;
    private bool previousJumping;

    private float physicsLogTimer;

    // Guardamos la velocidad anterior para detectar
    // cambios extraños.
    private float previousVelocityY;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerInteraction = GetComponent<PlayerInteraction>();

        controls = new PlayerControls();

        InputSettings.LoadBindings(controls);

        DebugLog(
            $"[INIT] Player={playerNumber} | " +
            $"Position={transform.position} | " +
            $"Rigidbody={rb != null}"
        );

        if (rb != null)
        {
            DebugLog(
                $"[RIGIDBODY] " +
                $"UseGravity={rb.useGravity} | " +
                $"IsKinematic={rb.isKinematic} | " +
                $"Mass={rb.mass} | " +
                $"Constraints={rb.constraints}"
            );
        }
    }

    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

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

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        CheckGround();

        // Detectar cambios de Grounded.
        if (isGrounded != previousGrounded)
        {
            DebugLog(
                $"[GROUND CHANGE] " +
                $"{previousGrounded} -> {isGrounded} | " +
                $"PositionY={transform.position.y:F3} | " +
                $"VelocityY={rb.linearVelocity.y:F3} | " +
                $"Jumping={isJumping}"
            );

            previousGrounded = isGrounded;
        }

        // Detectar cambios de Jumping.
        if (isJumping != previousJumping)
        {
            DebugLog(
                $"[JUMP STATE CHANGE] " +
                $"{previousJumping} -> {isJumping} | " +
                $"PositionY={transform.position.y:F3} | " +
                $"VelocityY={rb.linearVelocity.y:F3}"
            );

            previousJumping = isJumping;
        }

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
                jumpPressed =
                    Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            else
            {
                jumpPressed =
                    Keyboard.current.numpad0Key.wasPressedThisFrame;
            }
        }

        if (jumpPressed)
        {
            DebugLog(
                $"[JUMP START] " +
                $"Grounded={isGrounded} | " +
                $"Jumping={isJumping} | " +
                $"PositionY={transform.position.y:F3} | " +
                $"VelocityY BEFORE={rb.linearVelocity.y:F3} | " +
                $"JumpForce={jumpForce}"
            );

            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;

            isJumping = true;
            jumpTime = 0f;

            if (animator != null)
                animator.SetTrigger("Jump");

            DebugLog(
                $"[JUMP FORCE APPLIED] " +
                $"VelocityY AFTER={rb.linearVelocity.y:F3}"
            );
        }
    }

    // =========================================================
    // PHYSICS MOVEMENT
    // =========================================================

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        // =====================================================
        // DEBUG - ANTES DE MODIFICAR EL RIGIDBODY
        // =====================================================

        Vector3 velocity = rb.linearVelocity;

        float velocityYBeforeMovement = velocity.y;

        physicsLogTimer += Time.fixedDeltaTime;

        if (enableDebugLogs &&
            physicsLogTimer >= physicsLogInterval)
        {
            physicsLogTimer = 0f;

            DebugLog(
                $"[Y DEBUG BEFORE] " +
                $"Grounded={isGrounded} | " +
                $"Jumping={isJumping} | " +
                $"PosY={transform.position.y:F3} | " +
                $"VelocityY={velocity.y:F3} | " +
                $"PreviousVelocityY={previousVelocityY:F3} | " +
                $"InputY={verticalInput:F1}"
            );

            // =================================================
            // ALERTA 1
            // =================================================

            if (!isGrounded &&
                !isJumping &&
                velocity.y > 0.1f)
            {
                DebugLog(
                    $"[!!! ALERTA VERTICAL !!!] " +
                    $"El jugador NO está grounded, " +
                    $"NO está jumping, pero VelocityY es positiva: " +
                    $"{velocity.y:F3}"
                );
            }

            // =================================================
            // ALERTA 2
            // =================================================

            if (!isGrounded &&
                Mathf.Abs(velocity.y) < 0.01f)
            {
                DebugLog(
                    "[!!! ALERTA !!!] " +
                    "El jugador está en el aire pero VelocityY es prácticamente 0."
                );
            }

            // =================================================
            // ALERTA 3
            // =================================================

            if (Mathf.Abs(velocity.y - previousVelocityY) > 3f)
            {
                DebugLog(
                    $"[!!! CAMBIO VELOCIDAD !!!] " +
                    $"VelocityY cambió de " +
                    $"{previousVelocityY:F3} a {velocity.y:F3}"
                );
            }

            previousVelocityY = velocity.y;
        }

        // =====================================================
        // MOVEMENT
        // =====================================================

        float currentSpeed =
            isSprinting
                ? speed * sprintMultiplier
                : speed;

        Vector3 forward = transform.forward;

        Vector3 desiredVelocity =
            forward *
            verticalInput *
            currentSpeed;

        Vector3 desiredMovement =
            desiredVelocity *
            Time.fixedDeltaTime;

        // =====================================================
        // PLAYER INTERACTION
        // =====================================================

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

        // Guardamos X/Z.
        velocity.x = desiredVelocity.x;
        velocity.z = desiredVelocity.z;

        // =====================================================
        // RESET Y
        // =====================================================

        if (isGrounded && !isJumping)
        {
            if (Mathf.Abs(velocity.y) > 0.01f)
            {
                DebugLog(
                    $"[RESET Y] " +
                    $"Grounded={isGrounded} | " +
                    $"Jumping={isJumping} | " +
                    $"VelocityY BEFORE RESET={velocity.y:F3}"
                );
            }

            velocity.y = 0f;
        }

        // =====================================================
        // ASIGNAR VELOCIDAD
        // =====================================================

        rb.linearVelocity = velocity;

        // =====================================================
        // DEBUG - DESPUÉS DE MODIFICAR EL RIGIDBODY
        // =====================================================

        if (enableDebugLogs)
        {
            float velocityYAfterMovement =
                rb.linearVelocity.y;

            if (Mathf.Abs(
                    velocityYAfterMovement -
                    velocityYBeforeMovement
                ) > 0.01f)
            {
                DebugLog(
                    $"[Y DEBUG AFTER] " +
                    $"VelocityY BEFORE={velocityYBeforeMovement:F3} | " +
                    $"VelocityY AFTER={velocityYAfterMovement:F3} | " +
                    $"Grounded={isGrounded} | " +
                    $"Jumping={isJumping}"
                );
            }
        }

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
                    DebugLog(
                        $"[JUMP HOLD] " +
                        $"Time={jumpTime:F3}/{maxJumpTime:F3} | " +
                        $"VelocityY BEFORE={rb.linearVelocity.y:F3}"
                    );

                    rb.AddForce(
                        Vector3.up *
                        jumpHoldForce,
                        ForceMode.Acceleration
                    );

                    jumpTime +=
                        Time.fixedDeltaTime;

                    DebugLog(
                        $"[JUMP HOLD] " +
                        $"VelocityY AFTER={rb.linearVelocity.y:F3}"
                    );
                }
                else
                {
                    DebugLog(
                        "[JUMP END] MaxJumpTime alcanzado."
                    );

                    isJumping = false;
                }
            }
            else
            {
                DebugLog(
                    "[JUMP END] Botón de salto soltado."
                );

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

        bool newGrounded =
            detected &&
            hit.normal.y > 0.5f;

        // =====================================================
        // GROUND HIT
        // =====================================================

        if (detected)
        {
            DebugLog(
                $"[GROUND HIT] " +
                $"Collider={hit.collider.name} | " +
                $"Object={hit.collider.gameObject.name} | " +
                $"Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} | " +
                $"HitPoint={hit.point} | " +
                $"Normal={hit.normal} | " +
                $"NormalY={hit.normal.y:F3} | " +
                $"Distance={hit.distance:F3} | " +
                $"PlayerY={transform.position.y:F3}"
            );
        }

        // =====================================================
        // GROUND CHECK SOSPECHOSO
        // =====================================================

        if (newGrounded &&
            rb != null &&
            Mathf.Abs(rb.linearVelocity.y) > 0.5f)
        {
            DebugLog(
                $"[!!! GROUND CHECK SOSPECHOSO !!!] " +
                $"Grounded=TRUE mientras VelocityY=" +
                $"{rb.linearVelocity.y:F3} | " +
                $"Collider={hit.collider.name}"
            );
        }

        isGrounded = newGrounded;
    }

    // =========================================================
    // COLLISIONS
    // =========================================================

    private void OnCollisionStay(Collision collision)
    {
        // Ignorar bloques agarrados.
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

                DebugLog(
                    $"[WALL] " +
                    $"Object={collision.gameObject.name} | " +
                    $"Normal={normal}"
                );
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

    // =========================================================
    // DEBUG LOG
    // =========================================================

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log(
            $"[LocalPlayerMovement - {playerNumber}] {message}",
            this
        );
    }
}
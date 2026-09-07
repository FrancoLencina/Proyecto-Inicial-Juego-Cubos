using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.5f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpHoldForce = 15f;
    public float maxJumpTime = 0.4f;

    [Header("Footsteps")]
    public float footstepInterval = 0.4f;

    private float footstepTimer;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckHeight = 1f;
    public float groundCheckRadius = 0.25f;
    public float groundCheckDistance = 0.5f;

    [Header("Block Push")]
    [Tooltip("Multiplicador de fuerza con el que el jugador empuja los bloques.")]
    public float blockPushForce = 8f;

    [Tooltip("Tiempo mínimo entre empujones enviados al servidor para el mismo bloque.")]
    public float blockPushInterval = 0.05f;

    private Rigidbody rb;

    private NetworkPlayerInteraction playerInteraction;

    private PlayerControls controls;

    private Vector3 movement;

    private bool isSprinting;

    private bool isGrounded;

    private bool isJumping;

    private float jumpTime;

    private bool wasGrounded;

    private Vector3 wallNormal;

    private float pendingRotation;

    private bool gameEnded;

    private Dictionary<NetworkObject, float> lastBlockPushTimes =
        new Dictionary<NetworkObject, float>();


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        controls =
            new PlayerControls();

        InputSettings.LoadBindings(
            controls
        );
    }


    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    private void OnEnable()
    {
        controls.Enable();
    }


    private void OnDisable()
    {
        controls.Disable();
    }


    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        rb =
            GetComponent<Rigidbody>();

        playerInteraction =
            GetComponent<NetworkPlayerInteraction>();

        /*
         * Solo el jugador dueño controla
         * este personaje.
         */

        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!IsOwner)
            return;

        if (gameEnded)
            return;

        CheckGround();


        // =====================================================
        // INPUT DE MOVIMIENTO
        // =====================================================

        Vector2 moveInput =
            controls.Player.Move.ReadValue<Vector2>();

        float horizontal =
            moveInput.x;

        float vertical =
            moveInput.y;

        isSprinting =
            controls.Player.Sprint.IsPressed();

        movement =
            transform.right * horizontal +
            transform.forward * vertical;

        if (movement.magnitude > 1f)
            movement.Normalize();


        // =====================================================
        // FOOTSTEPS
        // =====================================================

        if (
            isGrounded &&
            movement.magnitude > 0.1f
        )
        {
            footstepTimer -=
                Time.deltaTime;

            if (
                footstepTimer <= 0f
            )
            {
                if (
                    SoundManager.Instance != null
                )
                {
                    SoundManager.Instance
                        .PlayFootstep();
                }

                footstepTimer =
                    footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }


        // =====================================================
        // COMENZAR SALTO
        // =====================================================

        if (
            controls.Player.Jump
                .WasPressedThisFrame() &&
            isGrounded
        )
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            if (
                SoundManager.Instance != null
            )
            {
                SoundManager.Instance
                    .PlayJump();
            }

            isGrounded = false;

            isJumping = true;

            jumpTime = 0f;
        }


        // =====================================================
        // SOLTAR ESPACIO
        // =====================================================

        if (
            controls.Player.Jump
                .WasReleasedThisFrame()
        )
        {
            isJumping = false;
        }
    }


    // =========================================================
    // SOLICITAR ROTACIÓN
    // =========================================================

    public void RequestRotation(
        float rotation
    )
    {
        if (!IsOwner)
            return;

        if (gameEnded)
            return;

        pendingRotation +=
            rotation;
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (gameEnded)
            return;

        Vector3 velocity =
            rb.linearVelocity;


        // =====================================================
        // MOVIMIENTO
        // =====================================================

        float currentSpeed =
            isSprinting
                ? speed * sprintMultiplier
                : speed;

        Vector3 desiredVelocity =
            movement * currentSpeed;

        Vector3 desiredMovement =
            desiredVelocity *
            Time.fixedDeltaTime;


        // =====================================================
        // BLOQUE SOSTENIDO
        // =====================================================

        if (
            playerInteraction != null &&
            playerInteraction.IsHoldingBlock
        )
        {
            desiredMovement =
                playerInteraction
                    .GetAllowedPlayerMovement(
                        desiredMovement
                    );
        }


        // =====================================================
        // CONVERTIR A VELOCIDAD
        // =====================================================

        if (
            Time.fixedDeltaTime > 0f
        )
        {
            desiredVelocity =
                desiredMovement /
                Time.fixedDeltaTime;
        }


        // =====================================================
        // EVITAR EMPUJAR CONTRA PAREDES
        // =====================================================

        if (
            wallNormal !=
            Vector3.zero
        )
        {
            float movementIntoWall =
                Vector3.Dot(
                    desiredVelocity,
                    wallNormal
                );

            if (
                movementIntoWall < 0f
            )
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

        if (
            Mathf.Abs(
                pendingRotation
            ) > 0.0001f
        )
        {
            float requestedRotation =
                pendingRotation;

            pendingRotation = 0f;

            float allowedRotation =
                requestedRotation;

            if (
                playerInteraction != null &&
                playerInteraction.IsHoldingBlock
            )
            {
                Vector3 correction =
                    playerInteraction
                        .GetRotationCorrection(
                            requestedRotation,
                            out allowedRotation
                        );

                if (
                    correction.sqrMagnitude >
                    0.000001f
                )
                {
                    rb.MovePosition(
                        rb.position +
                        correction
                    );
                }
            }

            if (
                Mathf.Abs(
                    allowedRotation
                ) > 0.0001f
            )
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

        if (
            isJumping &&
            controls.Player.Jump.IsPressed()
        )
        {
            if (
                jumpTime <
                maxJumpTime
            )
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
        // RESET WALL NORMAL
        // =====================================================

        wallNormal =
            Vector3.zero;
    }


    // =========================================================
    // DETECCIÓN DEL SUELO
    // =========================================================

    private void CheckGround()
    {
        wasGrounded =
            isGrounded;

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

        if (
            detected &&
            hit.normal.y > 0.5f
        )
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }


        // =====================================================
        // LANDING SOUND
        // =====================================================

        if (
            !wasGrounded &&
            isGrounded
        )
        {
            if (
                SoundManager.Instance != null
            )
            {
                SoundManager.Instance
                    .PlayLand();
            }
        }
    }


    // =========================================================
    // DETECCIÓN DE COLISIONES
    // =========================================================

    private void OnCollisionStay(
        Collision collision
    )
    {
        if (!IsOwner)
            return;

        if (gameEnded)
            return;

        if (collision == null)
            return;


        // -----------------------------------------------------
        // BUSCAR NETWORK FRUIT BLOCK
        // -----------------------------------------------------

        NetworkFruitBlock block =
            collision.gameObject
                .GetComponentInParent<
                    NetworkFruitBlock
                >();

        if (block != null)
        {
            HandleBlockCollision(
                collision,
                block
            );

            return;
        }


        // -----------------------------------------------------
        // BLOQUES HELD / FRUIT BLOCKS
        // -----------------------------------------------------

        if (
            collision.gameObject.layer ==
            LayerMask.NameToLayer(
                "HeldFruitBlock"
            )
        )
        {
            return;
        }

        if (
            collision.gameObject.layer ==
            LayerMask.NameToLayer(
                "FruitBlocks"
            )
        )
        {
            return;
        }


        // -----------------------------------------------------
        // PAREDES
        // -----------------------------------------------------

        foreach (
            ContactPoint contact
            in collision.contacts
        )
        {
            Vector3 normal =
                contact.normal;

            if (
                normal.y < 0.5f
            )
            {
                wallNormal =
                    normal;
            }
        }
    }


    // =========================================================
    // COLISIÓN CON BLOQUE
    // =========================================================

    private void HandleBlockCollision(
        Collision collision,
        NetworkFruitBlock block
    )
    {
        if (block == null)
            return;

        if (block.NetworkObject == null)
            return;


        // -----------------------------------------------------
        // NO EMPUJAR EL BLOQUE QUE ESTE JUGADOR SOSTIENE
        // -----------------------------------------------------

        if (
            block.IsBeingHeld &&
            block.HolderClientId ==
            NetworkManager.LocalClientId
        )
        {
            return;
        }


        // -----------------------------------------------------
        // DIRECCIÓN DEL MOVIMIENTO
        // -----------------------------------------------------

        Vector3 horizontalMovement =
            new Vector3(
                movement.x,
                0f,
                movement.z
            );

        if (
            horizontalMovement.sqrMagnitude <
            0.01f
        )
        {
            return;
        }


        // -----------------------------------------------------
        // DIRECCIÓN DEL EMPUJE
        // -----------------------------------------------------

        Vector3 pushDirection =
            Vector3.zero;

        foreach (
            ContactPoint contact
            in collision.contacts
        )
        {
            Vector3 direction =
                -contact.normal;

            direction.y = 0f;

            if (
                direction.sqrMagnitude >
                0.001f
            )
            {
                pushDirection =
                    direction.normalized;

                break;
            }
        }

        if (
            pushDirection ==
            Vector3.zero
        )
        {
            return;
        }


        // -----------------------------------------------------
        // COMPROBAR DIRECCIÓN DEL INPUT
        // -----------------------------------------------------

        Vector3 movementDirection =
            horizontalMovement.normalized;

        float movementIntoBlock =
            Vector3.Dot(
                movementDirection,
                pushDirection
            );

        if (
            movementIntoBlock <= 0.1f
        )
        {
            return;
        }


        // -----------------------------------------------------
        // LIMITAR FRECUENCIA
        // -----------------------------------------------------

        float currentTime =
            Time.time;

        if (
            lastBlockPushTimes.TryGetValue(
                block.NetworkObject,
                out float lastPushTime
            )
        )
        {
            if (
                currentTime -
                lastPushTime <
                blockPushInterval
            )
            {
                return;
            }
        }

        lastBlockPushTimes[
            block.NetworkObject
        ] = currentTime;


        // -----------------------------------------------------
        // CALCULAR IMPULSO
        // -----------------------------------------------------

        float pushStrength =
            speed *
            blockPushForce;

        Vector3 impulse =
            pushDirection *
            pushStrength;


        // -----------------------------------------------------
        // ENVIAR AL SERVIDOR
        // -----------------------------------------------------

        PushBlockServerRpc(
            block.NetworkObject,
            impulse
        );
    }


    // =========================================================
    // SERVER RPC - EMPUJAR BLOQUE
    // =========================================================

    [ServerRpc]
    private void PushBlockServerRpc(
        NetworkObjectReference blockReference,
        Vector3 impulse
    )
    {
        Debug.Log(
            "[BODY PUSH SERVER 1] RPC recibido"
        );

        if (
            !blockReference.TryGet(
                out NetworkObject networkObject
            )
        )
        {
            Debug.Log(
                "[BODY PUSH SERVER STOP] " +
                "No se encontró NetworkObject"
            );

            return;
        }

        NetworkFruitBlock block =
            networkObject.GetComponent<
                NetworkFruitBlock>();

        if (block == null)
        {
            Debug.Log(
                "[BODY PUSH SERVER STOP] " +
                "NetworkFruitBlock es null"
            );

            return;
        }

        Debug.Log(
            "[BODY PUSH SERVER 2] " +
            "Bloque encontrado | Held: " +
            block.IsBeingHeld
        );

        if (block.IsBeingHeld)
        {
            Debug.Log(
                "[BODY PUSH SERVER STOP] " +
                "Bloque sostenido"
            );

            return;
        }

        float maxImpulse = 3f;

        if (
            impulse.magnitude >
            maxImpulse
        )
        {
            impulse =
                impulse.normalized *
                maxImpulse;
        }

        Debug.Log(
            "[BODY PUSH SERVER 3] " +
            "Aplicando impulso: " +
            impulse
        );

        Debug.Log(
            "[BODY PUSH SERVER 4] " +
            "Llamando ApplyServerImpulse"
        );

        block.ApplyServerImpulse(
            impulse
        );

        Debug.Log(
            "[BODY PUSH SERVER 5] " +
            "ApplyServerImpulse finalizado"
        );
    }


    // =========================================================
    // CONGELAR AL FINAL DE LA PARTIDA
    // =========================================================

    public void FreezeForGameEnd()
    {
        if (!IsOwner)
            return;

        gameEnded = true;

        /*
         * Detener movimiento físico inmediatamente.
         */

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }

        movement =
            Vector3.zero;

        pendingRotation =
            0f;

        isJumping =
            false;

        isSprinting =
            false;

        footstepTimer =
            0f;

        /*
         * Desactivar el control.
         */

        controls.Disable();

        /*
         * Desactivar el componente para que no vuelva
         * a procesar movimiento.
         */

        enabled = false;

        Debug.Log(
            "[NetworkPlayerMovement] " +
            "Jugador congelado por final de partida."
        );
    }


    // =========================================================
    // PROPIEDAD DE SUELO
    // =========================================================

    public bool IsGrounded =>
        isGrounded;
}

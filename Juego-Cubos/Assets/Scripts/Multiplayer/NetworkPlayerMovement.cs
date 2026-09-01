using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class NetworkPlayerMovement : NetworkBehaviour
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

    [Header("Block Push")]
    [Tooltip("Multiplicador de fuerza con el que el jugador empuja los bloques.")]
    public float blockPushForce = 1.5f;

    [Tooltip("Tiempo mínimo entre empujones enviados al servidor para el mismo bloque.")]
    public float blockPushInterval = 0.08f;

    private Rigidbody rb;
    private PlayerInteraction playerInteraction;

    private Vector3 movement;

    private bool isGrounded;
    private bool isJumping;
    private float jumpTime;

    private Vector3 wallNormal;

    private float pendingRotation;


    // Guarda cuándo se envió el último impulso para cada bloque.
    private Dictionary<NetworkObject, float> lastBlockPushTimes =
        new Dictionary<NetworkObject, float>();


    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        playerInteraction =
            GetComponent<PlayerInteraction>();


        // Solo el jugador dueño puede controlar
        // este personaje.

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
        // Seguridad extra.
        if (!IsOwner)
            return;

        if (Keyboard.current == null)
            return;


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
        if (!IsOwner)
            return;

        pendingRotation += rotation;
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;


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


            if (playerInteraction != null &&
                playerInteraction.IsHoldingBlock)
            {
                Vector3 correction =
                    playerInteraction.GetRotationCorrection(
                        requestedRotation,
                        out allowedRotation
                    );


                if (correction.sqrMagnitude >
                    0.000001f)
                {
                    rb.MovePosition(
                        rb.position +
                        correction
                    );
                }
            }


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
    // DETECCIÓN DE COLISIONES
    // =========================================================

    private void OnCollisionStay(
        Collision collision)
    {
        if (!IsOwner)
            return;


        if (collision == null)
            return;


        // -----------------------------------------------------
        // BUSCAR NETWORK FRUIT BLOCK
        // -----------------------------------------------------

        NetworkFruitBlock block =
            collision.gameObject.GetComponentInParent<
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


        // -----------------------------------------------------
        // PAREDES
        // -----------------------------------------------------

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
    // COLISIÓN CON BLOQUE
    // =========================================================

    private void HandleBlockCollision(
        Collision collision,
        NetworkFruitBlock block)
    {
        if (block == null)
            return;


        if (block.NetworkObject == null)
            return;


        // -----------------------------------------------------
        // SI ESTE JUGADOR ESTÁ SOSTENIENDO EL BLOQUE
        // -----------------------------------------------------

        if (block.IsBeingHeld &&
            block.HolderClientId ==
            NetworkManager.LocalClientId)
        {
            return;
        }


        // -----------------------------------------------------
        // OBTENER VELOCIDAD
        // -----------------------------------------------------

        Vector3 playerVelocity =
            rb.linearVelocity;


        Vector3 horizontalVelocity =
            new Vector3(
                playerVelocity.x,
                0f,
                playerVelocity.z
            );


        if (horizontalVelocity.sqrMagnitude <
            0.01f)
        {
            return;
        }


        // -----------------------------------------------------
        // BUSCAR NORMAL DE COLISIÓN
        // -----------------------------------------------------

        Vector3 pushDirection =
            Vector3.zero;


        foreach (ContactPoint contact
                 in collision.contacts)
        {
            Vector3 normal =
                contact.normal;


            // La normal apunta del bloque hacia
            // el jugador.
            //
            // Por eso invertimos la normal para
            // obtener la dirección del empuje.

            Vector3 direction =
                -normal;


            direction.y = 0f;


            if (direction.sqrMagnitude >
                0.001f)
            {
                pushDirection =
                    direction.normalized;

                break;
            }
        }


        if (pushDirection == Vector3.zero)
            return;


        // -----------------------------------------------------
        // COMPROBAR QUE REALMENTE ESTAMOS EMPUJANDO
        // -----------------------------------------------------

        float movementIntoBlock =
            Vector3.Dot(
                horizontalVelocity,
                pushDirection
            );


        if (movementIntoBlock <= 0.05f)
        {
            return;
        }


        // -----------------------------------------------------
        // LIMITAR FRECUENCIA
        // -----------------------------------------------------

        float currentTime =
            Time.time;


        if (lastBlockPushTimes.TryGetValue(
            block.NetworkObject,
            out float lastPushTime))
        {
            if (currentTime - lastPushTime <
                blockPushInterval)
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
            movementIntoBlock *
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
        Vector3 impulse)
    {
        // -----------------------------------------------------
        // BUSCAR BLOQUE
        // -----------------------------------------------------

        if (!blockReference.TryGet(
            out NetworkObject networkObject))
        {
            return;
        }


        NetworkFruitBlock block =
            networkObject.GetComponent<
                NetworkFruitBlock
            >();


        if (block == null)
            return;


        // -----------------------------------------------------
        // NO EMPUJAR BLOQUES SOSTENIDOS
        // -----------------------------------------------------

        if (block.IsBeingHeld)
            return;


        // -----------------------------------------------------
        // LIMITAR IMPULSO
        // -----------------------------------------------------

        float maxImpulse =
            3f;


        if (impulse.magnitude >
            maxImpulse)
        {
            impulse =
                impulse.normalized *
                maxImpulse;
        }


        // -----------------------------------------------------
        // APLICAR FÍSICA EN SERVIDOR
        // -----------------------------------------------------

        block.ApplyServerImpulse(
            impulse
        );
    }
}

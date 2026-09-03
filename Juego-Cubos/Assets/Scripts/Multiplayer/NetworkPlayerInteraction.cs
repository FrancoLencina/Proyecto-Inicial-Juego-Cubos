using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public partial class NetworkPlayerInteraction : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fruitBlockLayer;
    [SerializeField] private Transform holdPoint;

    [Header("Held Block Collision")]
    [SerializeField] private LayerMask heldBlockBlockingLayers;

    [Header("Slope")]
    [SerializeField] private float slopeNormalThreshold = 0.5f;

    [Header("Rotation Correction")]
    [SerializeField] private float maxRotationCorrection = 0.25f;
    [SerializeField] private float rotationCorrectionStep = 0.01f;

    [Header("Rotation Prediction")]
    [SerializeField] private int rotationSamples = 30;

    [Header("Push")]
    [SerializeField] private float pushForce = 3f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 5f;

    [Tooltip("Radio de tolerancia para detectar bloques.")]
    [SerializeField] private float interactionRadius = 0.18f;


    // =========================================================
    // ESTADO DEL BLOQUE
    // =========================================================

    private NetworkFruitBlock heldBlock;

    private Rigidbody heldRigidbody;
    private BoxCollider heldCollider;


    // =========================================================
    // PROPIEDAD PÚBLICA
    // =========================================================

    public bool IsHoldingBlock =>
        heldBlock != null;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Solo el jugador propietario procesa input.

        if (!IsOwner)
            return;


        if (Keyboard.current == null)
            return;


        // -----------------------------------------------------
        // SI ESTÁ SOSTENIENDO UN BLOQUE
        // -----------------------------------------------------

        if (heldBlock != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RequestDropServerRpc();
            }

            return;
        }


        // -----------------------------------------------------
        // BUSCAR FRUITBLOCK
        // -----------------------------------------------------

        if (playerCamera == null)
            return;


        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        if (Physics.SphereCast(
            ray,
            interactionRadius,
            out RaycastHit hit,
            interactionDistance,
            fruitBlockLayer,
            QueryTriggerInteraction.Ignore
        ))
        {
            NetworkFruitBlock block =
                hit.collider.GetComponentInParent<
                    NetworkFruitBlock
                >();


            if (block == null)
                return;


            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RequestGrabServerRpc(
                    block.NetworkObject
                );
            }
        }
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        // Solo el jugador propietario procesa
        // el bloque que está sosteniendo.

        if (!IsOwner)
            return;


        if (heldBlock == null ||
            heldRigidbody == null ||
            heldCollider == null)
        {
            return;
        }


        if (holdPoint == null)
            return;


        // El bloque debe pertenecer a este jugador.

        if (!heldBlock.NetworkObject.IsOwner)
            return;


        // =====================================================
        // POSICIÓN ACTUAL Y OBJETIVO
        // =====================================================

        Vector3 currentPosition =
            heldRigidbody.position;


        Vector3 targetPosition =
            holdPoint.position;


        Quaternion targetRotation =
            holdPoint.rotation;


        Vector3 movement =
            targetPosition -
            currentPosition;


        // =====================================================
        // DETECTAR DESCENSO DEL BLOQUE
        // =====================================================

        if (movement.y < -0.0001f)
        {
            if (ShouldDropBecauseOfSurface(
                currentPosition,
                targetRotation,
                movement
            ))
            {
                return;
            }
        }


        // =====================================================
        // POSICIÓN SEGURA
        // =====================================================

        Vector3 safePosition =
            GetSafeBlockPosition(
                currentPosition,
                targetPosition,
                targetRotation
            );


        // =====================================================
        // MOVIMIENTO REAL
        // =====================================================

        Vector3 actualMovement =
            safePosition -
            currentPosition;


        heldRigidbody.MovePosition(
            safePosition
        );


        heldRigidbody.MoveRotation(
            targetRotation
        );


        // =====================================================
        // EMPUJAR OTROS FRUITBLOCKS
        // =====================================================

        PushNearbyFruitBlocks(
            actualMovement
        );
    }

    // =========================================================
    // CAMERA
    // =========================================================

    public void SetPlayerCamera(
        Camera newCamera)
    {
        playerCamera = newCamera;
    }


    // =========================================================
    // HOLD POINT
    // =========================================================

    public void SetHoldPoint(
        Transform newHoldPoint)
    {
        holdPoint = newHoldPoint;
    }

    // =========================================================
    // REQUEST GRAB
    // =========================================================

    [ServerRpc]
    private void RequestGrabServerRpc(
        NetworkObjectReference blockReference,
        ServerRpcParams rpcParams = default
    )
    {
        // -----------------------------------------------------
        // BUSCAR BLOQUE
        // -----------------------------------------------------

        if (!blockReference.TryGet(
            out NetworkObject networkObject
        ))
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
        // COMPROBAR SI YA ESTÁ AGARRADO
        // -----------------------------------------------------

        if (block.IsBeingHeld)
            return;


        // -----------------------------------------------------
        // TRANSFERIR OWNERSHIP AL JUGADOR
        // -----------------------------------------------------

        networkObject.ChangeOwnership(
            rpcParams.Receive.SenderClientId
        );


        // -----------------------------------------------------
        // ACTUALIZAR ESTADO DEL BLOQUE
        // -----------------------------------------------------

        block.SetHeldState(
            true,
            rpcParams.Receive.SenderClientId
        );


        // -----------------------------------------------------
        // INFORMAR AL JUGADOR
        // -----------------------------------------------------

        SetHeldBlockClientRpc(
            blockReference,
            true,
            rpcParams.Receive.SenderClientId
        );
    }


    // =========================================================
    // REQUEST DROP
    // =========================================================

    [ServerRpc]
    private void RequestDropServerRpc(
        ServerRpcParams rpcParams = default
    )
    {
        // -----------------------------------------------------
        // VERIFICAR QUE EL JUGADOR TENGA UN BLOQUE
        // -----------------------------------------------------

        if (heldBlock == null)
            return;


        NetworkObject networkObject =
            heldBlock.NetworkObject;


        if (networkObject == null)
            return;


        // -----------------------------------------------------
        // VERIFICAR OWNERSHIP
        // -----------------------------------------------------

        if (networkObject.OwnerClientId !=
            rpcParams.Receive.SenderClientId)
        {
            return;
        }


        NetworkObjectReference blockReference =
            new NetworkObjectReference(
                networkObject
            );


        // -----------------------------------------------------
        // ACTUALIZAR ESTADO
        // -----------------------------------------------------

        heldBlock.SetHeldState(
            false,
            NetworkManager.ServerClientId
        );


        // -----------------------------------------------------
        // DEVOLVER OWNERSHIP AL SERVIDOR
        // -----------------------------------------------------

        networkObject.ChangeOwnership(
            NetworkManager.ServerClientId
        );


        // -----------------------------------------------------
        // INFORMAR A LOS CLIENTES
        // -----------------------------------------------------

        ClearHeldBlockClientRpc(
    rpcParams.Receive.SenderClientId
);
    }


    // =========================================================
    // SINCRONIZAR BLOQUE SOSTENIDO
    // =========================================================

    [ClientRpc]
    private void SetHeldBlockClientRpc(
        NetworkObjectReference blockReference,
        bool isHeld,
        ulong holderClientId
    )
    {
        // Solo el jugador correspondiente debe
        // modificar su referencia local.

        if (NetworkManager.LocalClientId !=
            holderClientId)
        {
            return;
        }


        // =====================================================
        // AGARRAR
        // =====================================================

        if (isHeld)
        {
            if (!blockReference.TryGet(
                out NetworkObject networkObject
            ))
            {
                return;
            }


            NetworkFruitBlock block =
                networkObject.GetComponent<
                    NetworkFruitBlock
                >();


            if (block == null)
                return;


            heldBlock =
                block;


            heldRigidbody =
                block.GetComponent<Rigidbody>();


            heldCollider =
                block.GetComponent<BoxCollider>();


            if (heldRigidbody != null)
            {
                heldRigidbody.linearVelocity =
                    Vector3.zero;

                heldRigidbody.angularVelocity =
                    Vector3.zero;

                heldRigidbody.position =
                    holdPoint.position;

                heldRigidbody.rotation =
                    holdPoint.rotation;
            }


            return;
        }


        // =====================================================
        // SOLTAR
        // =====================================================

        heldBlock =
            null;

        heldRigidbody =
            null;

        heldCollider =
            null;
    }

    // =========================================================
    // LIMPIAR BLOQUE SOSTENIDO
    // =========================================================

    [ClientRpc]
    private void ClearHeldBlockClientRpc(
        ulong holderClientId
    )
    {
        if (NetworkManager.LocalClientId !=
            holderClientId)
        {
            return;
        }


        heldBlock =
            null;

        heldRigidbody =
            null;

        heldCollider =
            null;
    }

}
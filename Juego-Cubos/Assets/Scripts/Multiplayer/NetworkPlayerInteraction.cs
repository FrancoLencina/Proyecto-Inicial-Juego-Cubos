using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class NetworkPlayerInteraction : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fruitBlockLayer;
    [SerializeField] private Transform holdPoint;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 5f;

    [Tooltip("Radio de tolerancia para detectar bloques.")]
    [SerializeField] private float interactionRadius = 0.18f;

    private NetworkFruitBlock heldBlock;

    public bool IsHoldingBlock => heldBlock != null;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Solo el jugador local procesa sus controles.
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
        // BUSCAR BLOQUE
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


        // SphereCast para facilitar el agarre.
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
                hit.collider.GetComponentInParent<NetworkFruitBlock>();

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

    private void FixedUpdate()
    {
        // El servidor controla el movimiento
        // del bloque.
        if (!IsServer)
            return;

        if (heldBlock == null)
            return;

        if (holdPoint == null)
            return;


        Rigidbody blockRigidbody =
            heldBlock.GetComponent<Rigidbody>();

        if (blockRigidbody == null)
            return;


        // -----------------------------------------------------
        // POSICIÓN EXACTA DEL HOLD POINT
        // -----------------------------------------------------

        blockRigidbody.position =
            holdPoint.position;

        blockRigidbody.rotation =
            holdPoint.rotation;
    }


    // =========================================================
    // GRAB
    // =========================================================

    [ServerRpc]
    private void RequestGrabServerRpc(
        NetworkObjectReference blockReference)
    {
        // -----------------------------------------------------
        // BUSCAR NETWORK OBJECT
        // -----------------------------------------------------

        if (!blockReference.TryGet(
            out NetworkObject networkObject))
        {
            Debug.LogWarning(
                "[NetworkPlayerInteraction] " +
                "No se pudo encontrar el NetworkObject del bloque."
            );

            return;
        }


        NetworkFruitBlock block =
            networkObject.GetComponent<NetworkFruitBlock>();

        if (block == null)
        {
            Debug.LogWarning(
                "[NetworkPlayerInteraction] " +
                "El objeto no tiene NetworkFruitBlock."
            );

            return;
        }


        // -----------------------------------------------------
        // EVITAR DOS BLOQUES A LA VEZ
        // -----------------------------------------------------

        if (heldBlock != null)
        {
            return;
        }


        // -----------------------------------------------------
        // EVITAR DOS JUGADORES SOBRE EL MISMO BLOQUE
        // -----------------------------------------------------

        if (block.IsBeingHeld)
        {
            return;
        }


        // -----------------------------------------------------
        // AGARRAR
        // -----------------------------------------------------

        heldBlock = block;

        block.SetHeldState(
            true,
            OwnerClientId
        );


        // -----------------------------------------------------
        // DESACTIVAR FÍSICA
        // -----------------------------------------------------

        Rigidbody blockRigidbody =
            block.GetComponent<Rigidbody>();

        if (blockRigidbody != null)
        {
            blockRigidbody.linearVelocity =
                Vector3.zero;

            blockRigidbody.angularVelocity =
                Vector3.zero;

            blockRigidbody.isKinematic =
                true;
        }


        // -----------------------------------------------------
        // IGNORAR COLISIÓN CON EL PLAYER
        // -----------------------------------------------------

        IgnorePlayerBlockCollision(
            block,
            true
        );


        // Aplicar también la modificación
        // en el cliente propietario.
        SetPlayerBlockCollisionClientRpc(
            block.NetworkObject,
            true
        );


        Debug.Log(
            "[NetworkPlayerInteraction] " +
            "Cliente " +
            OwnerClientId +
            " agarró " +
            block.FruitType
        );


        // -----------------------------------------------------
        // INFORMAR A LOS CLIENTES
        // -----------------------------------------------------

        SetHeldBlockClientRpc(
            block.NetworkObject
        );
    }


    // =========================================================
    // DROP
    // =========================================================

    [ServerRpc]
    private void RequestDropServerRpc()
    {
        if (heldBlock == null)
            return;


        NetworkFruitBlock block =
            heldBlock;

        heldBlock = null;


        // -----------------------------------------------------
        // MARCAR COMO LIBRE
        // -----------------------------------------------------

        block.SetHeldState(
            false,
            0
        );


        // -----------------------------------------------------
        // RESTAURAR FÍSICA
        // -----------------------------------------------------

        Rigidbody blockRigidbody =
            block.GetComponent<Rigidbody>();

        if (blockRigidbody != null)
        {
            blockRigidbody.isKinematic =
                false;
        }


        // -----------------------------------------------------
        // RESTAURAR COLISIÓN
        // -----------------------------------------------------

        IgnorePlayerBlockCollision(
            block,
            false
        );


        SetPlayerBlockCollisionClientRpc(
            block.NetworkObject,
            false
        );


        Debug.Log(
            "[NetworkPlayerInteraction] " +
            "Cliente " +
            OwnerClientId +
            " soltó el bloque."
        );


        // -----------------------------------------------------
        // INFORMAR A LOS CLIENTES
        // -----------------------------------------------------

        ClearHeldBlockClientRpc();
    }


    // =========================================================
    // IGNORAR COLISIÓN PLAYER ↔ BLOQUE
    // =========================================================

    private void IgnorePlayerBlockCollision(
        NetworkFruitBlock block,
        bool ignore)
    {
        if (block == null)
            return;


        Collider[] playerColliders =
            GetComponentsInChildren<Collider>();

        Collider[] blockColliders =
            block.GetComponentsInChildren<Collider>();


        foreach (Collider playerCollider in playerColliders)
        {
            foreach (Collider blockCollider in blockColliders)
            {
                if (playerCollider == null ||
                    blockCollider == null)
                {
                    continue;
                }


                Physics.IgnoreCollision(
                    playerCollider,
                    blockCollider,
                    ignore
                );
            }
        }
    }


    // =========================================================
    // CLIENT RPC - COLISIÓN
    // =========================================================

    [ClientRpc]
    private void SetPlayerBlockCollisionClientRpc(
        NetworkObjectReference blockReference,
        bool ignore)
    {
        // Solo el jugador propietario modifica
        // su propia colisión.
        if (!IsOwner)
            return;


        if (!blockReference.TryGet(
            out NetworkObject networkObject))
        {
            return;
        }


        NetworkFruitBlock block =
            networkObject.GetComponent<NetworkFruitBlock>();

        if (block == null)
            return;


        IgnorePlayerBlockCollision(
            block,
            ignore
        );
    }


    // =========================================================
    // CLIENT RPC - GRAB
    // =========================================================

    [ClientRpc]
    private void SetHeldBlockClientRpc(
        NetworkObjectReference blockReference)
    {
        if (!blockReference.TryGet(
            out NetworkObject networkObject))
        {
            return;
        }


        NetworkFruitBlock block =
            networkObject.GetComponent<NetworkFruitBlock>();

        if (block == null)
            return;


        // Solamente el jugador que agarró
        // guarda la referencia local.
        if (IsOwner)
        {
            heldBlock = block;
        }
    }


    // =========================================================
    // CLIENT RPC - DROP
    // =========================================================

    [ClientRpc]
    private void ClearHeldBlockClientRpc()
    {
        if (IsOwner)
        {
            heldBlock = null;
        }
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
}

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
        // Solo el jugador propietario del Player
        // controla la interacción.
        if (!IsOwner)
            return;

        if (heldBlock == null)
            return;

        if (holdPoint == null)
            return;


        // El bloque debe pertenecer a este cliente.
        if (!heldBlock.NetworkObject.IsOwner)
            return;


        // -----------------------------------------------------
        // MOVER BLOQUE
        // -----------------------------------------------------

        // El bloque es Kinematic mientras está agarrado.
        //
        // Lo movemos directamente al HoldPoint para evitar
        // fuerzas físicas que puedan empujar otros bloques.

        heldBlock.transform.SetPositionAndRotation(
            holdPoint.position,
            holdPoint.rotation
        );
    }


    // =========================================================
    // GRAB
    // =========================================================

    [ServerRpc]
    private void RequestGrabServerRpc(
        NetworkObjectReference blockReference,
        ServerRpcParams rpcParams = default)
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
        // CLIENTE QUE INTENTA AGARRAR
        // -----------------------------------------------------

        ulong grabbingClientId =
            rpcParams.Receive.SenderClientId;


        // -----------------------------------------------------
        // EVITAR DOS JUGADORES SOBRE EL MISMO BLOQUE
        // -----------------------------------------------------

        if (block.IsBeingHeld)
        {
            return;
        }


        // -----------------------------------------------------
        // DETENER FÍSICA ANTES DE TRANSFERIR OWNERSHIP
        // -----------------------------------------------------

        SetBlockPhysics(
            block,
            true
        );


        // -----------------------------------------------------
        // MARCAR COMO SOSTENIDO
        // -----------------------------------------------------

        block.SetHeldState(
            true,
            grabbingClientId
        );


        // -----------------------------------------------------
        // TRANSFERIR OWNERSHIP
        // -----------------------------------------------------

        block.NetworkObject.ChangeOwnership(
            grabbingClientId
        );


        // -----------------------------------------------------
        // INFORMAR FÍSICA A TODOS LOS CLIENTES
        // -----------------------------------------------------

        SetBlockPhysicsClientRpc(
            block.NetworkObject,
            true,
            grabbingClientId
        );


        // -----------------------------------------------------
        // INFORMAR QUIÉN TIENE EL BLOQUE
        // -----------------------------------------------------

        SetHeldBlockClientRpc(
            block.NetworkObject,
            grabbingClientId
        );


        Debug.Log(
            "[NetworkPlayerInteraction] " +
            "Cliente " +
            grabbingClientId +
            " agarró " +
            block.FruitType
        );
    }


    // =========================================================
    // DROP
    // =========================================================

    [ServerRpc]
    private void RequestDropServerRpc(
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;


        // -----------------------------------------------------
        // BUSCAR BLOQUE DEL JUGADOR
        // -----------------------------------------------------

        NetworkFruitBlock block =
            FindHeldBlockByClientId(
                senderClientId
            );


        if (block == null)
        {
            return;
        }


        // -----------------------------------------------------
        // VALIDAR HOLDER
        // -----------------------------------------------------

        if (!block.IsBeingHeld)
        {
            return;
        }


        if (block.HolderClientId != senderClientId)
        {
            return;
        }


        // -----------------------------------------------------
        // DEVOLVER OWNERSHIP AL SERVIDOR
        // -----------------------------------------------------

        block.NetworkObject.ChangeOwnership(
            NetworkManager.ServerClientId
        );


        // -----------------------------------------------------
        // MARCAR COMO LIBRE
        // -----------------------------------------------------

        block.SetHeldState(
            false,
            0
        );


        // -----------------------------------------------------
        // RESTAURAR FÍSICA EN EL SERVIDOR
        // -----------------------------------------------------

        SetBlockPhysics(
            block,
            false
        );


        // -----------------------------------------------------
        // RESTAURAR FÍSICA EN LOS CLIENTES
        // -----------------------------------------------------

        SetBlockPhysicsClientRpc(
            block.NetworkObject,
            false,
            senderClientId
        );


        // -----------------------------------------------------
        // INFORMAR AL JUGADOR QUE SOLTÓ
        // -----------------------------------------------------

        ClearHeldBlockClientRpc(
            block.NetworkObject,
            senderClientId
        );


        Debug.Log(
            "[NetworkPlayerInteraction] " +
            "Cliente " +
            senderClientId +
            " soltó el bloque."
        );
    }


    // =========================================================
    // BUSCAR BLOQUE SOSTENIDO
    // =========================================================

    private NetworkFruitBlock FindHeldBlockByClientId(
        ulong clientId)
    {
        NetworkFruitBlock[] blocks =
            FindObjectsByType<NetworkFruitBlock>();


        foreach (NetworkFruitBlock block in blocks)
        {
            if (block == null)
                continue;


            if (!block.IsBeingHeld)
                continue;


            if (block.HolderClientId == clientId)
            {
                return block;
            }
        }


        return null;
    }


    // =========================================================
    // FÍSICA DEL BLOQUE
    // =========================================================

    private void SetBlockPhysics(
        NetworkFruitBlock block,
        bool held)
    {
        if (block == null)
            return;


        Rigidbody rb =
            block.GetComponent<Rigidbody>();


        if (rb == null)
            return;


        if (held)
        {
            // Eliminar cualquier velocidad antes de
            // convertirlo en Kinematic.

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.isKinematic =
                true;

            rb.useGravity =
                false;
        }
        else
        {
            // Eliminar velocidades residuales antes
            // de devolver la física.

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.isKinematic =
                false;

            rb.useGravity =
                true;
        }
    }


    // =========================================================
    // CLIENT RPC - FÍSICA
    // =========================================================

    [ClientRpc]
    private void SetBlockPhysicsClientRpc(
        NetworkObjectReference blockReference,
        bool held,
        ulong holderClientId)
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


        // -----------------------------------------------------
        // CONFIGURAR RIGIDBODY
        // -----------------------------------------------------

        SetBlockPhysics(
            block,
            held
        );


        // -----------------------------------------------------
        // COLISIÓN DEL JUGADOR CON SU PROPIO BLOQUE
        // -----------------------------------------------------

        if (holderClientId ==
            NetworkManager.LocalClientId)
        {
            IgnorePlayerBlockCollision(
                block,
                held
            );
        }
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
    // CLIENT RPC - GRAB
    // =========================================================

    [ClientRpc]
    private void SetHeldBlockClientRpc(
        NetworkObjectReference blockReference,
        ulong grabbingClientId)
    {
        // Solo el jugador que agarró el bloque
        // guarda la referencia local.

        if (grabbingClientId !=
            NetworkManager.LocalClientId)
        {
            return;
        }


        if (!blockReference.TryGet(
            out NetworkObject networkObject))
        {
            return;
        }


        NetworkFruitBlock block =
            networkObject.GetComponent<NetworkFruitBlock>();


        if (block == null)
            return;


        heldBlock = block;


        // Asegurar que la colisión con el jugador
        // quede desactivada.

        IgnorePlayerBlockCollision(
            block,
            true
        );
    }


    // =========================================================
    // CLIENT RPC - DROP
    // =========================================================

    [ClientRpc]
    private void ClearHeldBlockClientRpc(
        NetworkObjectReference blockReference,
        ulong previousHolderClientId)
    {
        // Solo el jugador que tenía el bloque
        // limpia su referencia.

        if (previousHolderClientId !=
            NetworkManager.LocalClientId)
        {
            return;
        }


        if (!blockReference.TryGet(
            out NetworkObject networkObject))
        {
            return;
        }


        NetworkFruitBlock block =
            networkObject.GetComponent<NetworkFruitBlock>();


        if (block == null)
            return;


        // Restaurar colisión.

        IgnorePlayerBlockCollision(
            block,
            false
        );


        // Limpiar referencia local.

        if (heldBlock == block)
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

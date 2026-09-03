using UnityEngine;
using Unity.Netcode;

public partial class NetworkPlayerInteraction : NetworkBehaviour
{
    // =========================================================
    // DETERMINAR SI EL BLOQUE DEBE SOLTARSE
    // =========================================================

    private bool ShouldDropBecauseOfSurface(
        Vector3 currentPosition,
        Quaternion targetRotation,
        Vector3 movement
    )
    {
        if (heldBlock == null ||
            heldCollider == null)
        {
            return false;
        }


        if (movement.sqrMagnitude <
            0.000001f)
        {
            return false;
        }


        Vector3 direction =
            movement.normalized;


        float distance =
            movement.magnitude;


        Vector3 halfExtents =
            GetWorldHalfExtents();


        // =====================================================
        // 1. SUPERFICIES DEL ESCENARIO
        // =====================================================

        RaycastHit[] hits =
            Physics.BoxCastAll(
                currentPosition,
                halfExtents,
                direction,
                targetRotation,
                distance,
                heldBlockBlockingLayers,
                QueryTriggerInteraction.Ignore
            );


        foreach (RaycastHit hit in hits)
        {
            if (IsHeldBlockCollider(
                hit.collider
            ))
            {
                continue;
            }


            if (hit.normal.y > 0.5f)
            {
                RequestDropServerRpc();
                return true;
            }
        }


        // =====================================================
        // 2. OTROS FRUITBLOCKS
        // =====================================================

        int fruitBlockLayer =
            LayerMask.NameToLayer(
                "FruitBlocks"
            );


        if (fruitBlockLayer >= 0)
        {
            int fruitBlockMask =
                1 << fruitBlockLayer;


            RaycastHit[] fruitHits =
                Physics.BoxCastAll(
                    currentPosition,
                    halfExtents,
                    direction,
                    targetRotation,
                    distance,
                    fruitBlockMask,
                    QueryTriggerInteraction.Ignore
                );


            foreach (RaycastHit hit in fruitHits)
            {
                if (IsHeldBlockCollider(
                    hit.collider
                ))
                {
                    continue;
                }


                if (hit.normal.y > 0.5f)
                {
                    RequestDropServerRpc();
                    return true;
                }
            }
        }


        return false;
    }


    // =========================================================
    // EMPUJAR FRUITBLOCKS
    // =========================================================

    private void PushNearbyFruitBlocks(
        Vector3 blockMovement
    )
    {
        if (!IsOwner)
            return;


        if (heldBlock == null ||
            heldRigidbody == null)
        {
            return;
        }


        if (blockMovement.sqrMagnitude <
            0.000001f)
        {
            return;
        }


        Vector3 halfExtents =
            GetWorldHalfExtents();


        Collider[] nearbyObjects =
            Physics.OverlapBox(
                heldRigidbody.position,
                halfExtents,
                heldRigidbody.rotation,
                LayerMask.GetMask(
                    "FruitBlocks"
                ),
                QueryTriggerInteraction.Ignore
            );


        foreach (
            Collider collider
            in nearbyObjects
        )
        {
            if (IsHeldBlockCollider(
                collider
            ))
            {
                continue;
            }


            NetworkFruitBlock otherBlock =
                collider.GetComponentInParent<
                    NetworkFruitBlock
                >();


            if (otherBlock == null)
            {
                continue;
            }


            // No empujar bloques que otro jugador
            // está sosteniendo.

            if (otherBlock.IsBeingHeld)
            {
                continue;
            }


            Vector3 pushDirection =
                blockMovement.normalized;


            // -------------------------------------------------
            // NO EMPUJAR VERTICALMENTE
            // -------------------------------------------------

            pushDirection.y =
                0f;


            if (pushDirection.sqrMagnitude <
                0.000001f)
            {
                continue;
            }


            pushDirection.Normalize();


            float movementAmount =
                blockMovement.magnitude;


            float force =
                pushForce *
                Mathf.Clamp01(
                    movementAmount /
                    Time.fixedDeltaTime
                );


            Vector3 impulse =
                pushDirection *
                force;


            // -------------------------------------------------
            // ENVIAR AL SERVIDOR
            // -------------------------------------------------

            PushHeldBlockServerRpc(
                otherBlock.NetworkObject,
                impulse
            );
        }
    }


    // =========================================================
    // SERVER RPC - EMPUJAR BLOQUE CON BLOQUE SOSTENIDO
    // =========================================================

    [ServerRpc]
    private void PushHeldBlockServerRpc(
        NetworkObjectReference blockReference,
        Vector3 impulse
    )
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
        {
            return;
        }


        if (block.IsBeingHeld)
        {
            return;
        }


        Rigidbody rb =
            block.GetComponent<Rigidbody>();


        if (rb == null ||
            rb.isKinematic)
        {
            return;
        }


        // Limitar fuerzas excesivas.

        float maxForce =
            10f;


        if (impulse.magnitude >
            maxForce)
        {
            impulse =
                impulse.normalized *
                maxForce;
        }


        rb.AddForce(
            impulse,
            ForceMode.Force
        );
    }
}
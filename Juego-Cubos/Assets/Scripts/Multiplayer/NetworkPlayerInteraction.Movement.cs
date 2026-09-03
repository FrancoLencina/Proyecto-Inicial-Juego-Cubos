using UnityEngine;
using Unity.Netcode;

public partial class NetworkPlayerInteraction : NetworkBehaviour
{
    // =========================================================
    // MOVIMIENTO PERMITIDO DEL PLAYER
    // =========================================================

    public Vector3 GetAllowedPlayerMovement(
        Vector3 playerMovement
    )
    {
        if (!IsHoldingBlock ||
            heldCollider == null ||
            heldBlock == null)
        {
            return playerMovement;
        }


        if (playerMovement.sqrMagnitude <
            0.000001f)
        {
            return playerMovement;
        }


        Vector3 currentBlockPosition =
            holdPoint.position;


        Quaternion rotation =
            holdPoint.rotation;


        Vector3 halfExtents =
            GetWorldHalfExtents();


        float distance =
            playerMovement.magnitude;


        Vector3 direction =
            playerMovement.normalized;


        // =====================================================
        // CAST DEL BLOQUE
        // =====================================================

        if (Physics.BoxCast(
            currentBlockPosition,
            halfExtents,
            direction,
            out RaycastHit hit,
            rotation,
            distance,
            heldBlockBlockingLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            if (!IsHeldBlockCollider(
                hit.collider
            ))
            {
                // -------------------------------------------------
                // PENDIENTE
                // -------------------------------------------------

                if (IsWalkableSurface(
                    hit.normal
                ))
                {
                    return playerMovement;
                }


                // -------------------------------------------------
                // PARED / OBSTÁCULO
                // -------------------------------------------------

                float safeDistance =
                    Mathf.Max(
                        0f,
                        hit.distance - 0.005f
                    );


                Vector3 allowed =
                    direction *
                    safeDistance;


                return allowed;
            }
        }


        // =====================================================
        // COMPROBACIÓN FINAL
        // =====================================================

        Vector3 predictedPosition =
            currentBlockPosition +
            playerMovement;


        if (IsPositionBlocked(
            predictedPosition,
            rotation
        ))
        {
            return Vector3.zero;
        }


        return playerMovement;
    }


    // =========================================================
    // DETERMINAR SI UNA SUPERFICIE ES CAMINABLE
    // =========================================================

    private bool IsWalkableSurface(
        Vector3 normal
    )
    {
        return normal.y >
               slopeNormalThreshold;
    }


    // =========================================================
    // POSICIÓN BLOQUEADA
    // =========================================================

    private bool IsPositionBlocked(
        Vector3 position,
        Quaternion rotation
    )
    {
        if (heldCollider == null ||
            heldBlock == null)
        {
            return false;
        }


        Vector3 halfExtents =
            GetWorldHalfExtents();


        Collider[] overlaps =
            Physics.OverlapBox(
                position,
                halfExtents,
                rotation,
                heldBlockBlockingLayers,
                QueryTriggerInteraction.Ignore
            );


        foreach (
            Collider collider
            in overlaps
        )
        {
            if (IsHeldBlockCollider(
                collider
            ))
            {
                continue;
            }


            // =================================================
            // COMPUTAR PENETRACIÓN
            // =================================================

            if (!Physics.ComputePenetration(
                heldCollider,
                position,
                rotation,

                collider,
                collider.transform.position,
                collider.transform.rotation,

                out Vector3 direction,
                out float distance
            ))
            {
                continue;
            }


            // =================================================
            // DETERMINAR SUPERFICIE
            // =================================================

            Vector3 normal =
                direction.normalized;


            if (IsWalkableSurface(normal))
            {
                continue;
            }


            return true;
        }


        return false;
    }


    // =========================================================
    // POSICIÓN SEGURA DEL BLOQUE
    // =========================================================

    private Vector3 GetSafeBlockPosition(
        Vector3 currentPosition,
        Vector3 targetPosition,
        Quaternion rotation
    )
    {
        if (heldCollider == null ||
            heldBlock == null)
        {
            return currentPosition;
        }


        Vector3 movement =
            targetPosition -
            currentPosition;


        float distance =
            movement.magnitude;


        if (distance <=
            0.0001f)
        {
            return currentPosition;
        }


        Vector3 direction =
            movement.normalized;


        Vector3 halfExtents =
            GetWorldHalfExtents();


        // =====================================================
        // BOXCAST
        // =====================================================

        if (Physics.BoxCast(
            currentPosition,
            halfExtents,
            direction,
            out RaycastHit hit,
            rotation,
            distance,
            heldBlockBlockingLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            if (!IsHeldBlockCollider(
                hit.collider
            ))
            {
                // -------------------------------------------------
                // PENDIENTE
                // -------------------------------------------------

                if (IsWalkableSurface(
                    hit.normal
                ))
                {
                    return targetPosition;
                }


                // -------------------------------------------------
                // PARED / OBSTÁCULO
                // -------------------------------------------------

                float safeDistance =
                    Mathf.Max(
                        0f,
                        hit.distance - 0.005f
                    );


                return
                    currentPosition +
                    direction *
                    safeDistance;
            }
        }


        // =====================================================
        // COMPROBACIÓN FINAL
        // =====================================================

        if (IsPositionBlocked(
            targetPosition,
            rotation
        ))
        {
            return currentPosition;
        }


        return targetPosition;
    }


    // =========================================================
    // TAMAÑO DEL COLLIDER
    // =========================================================

    private Vector3 GetWorldHalfExtents()
    {
        if (heldCollider == null ||
            heldBlock == null)
        {
            return Vector3.zero;
        }


        return Vector3.Scale(
            heldCollider.size * 0.5f,
            heldBlock.transform.lossyScale
        );
    }


    // =========================================================
    // IDENTIFICAR COLLIDER DEL BLOQUE SOSTENIDO
    // =========================================================

    private bool IsHeldBlockCollider(
        Collider collider
    )
    {
        if (collider == null ||
            heldBlock == null)
        {
            return false;
        }


        return collider.transform.IsChildOf(
                   heldBlock.transform
               ) ||
               collider.transform ==
               heldBlock.transform;
    }
}
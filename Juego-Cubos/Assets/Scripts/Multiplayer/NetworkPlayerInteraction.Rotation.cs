using UnityEngine;
using Unity.Netcode;

public partial class NetworkPlayerInteraction : NetworkBehaviour
{
    // =========================================================
    // CORRECCIÓN DE ROTACIÓN
    // =========================================================

    public Vector3 GetRotationCorrection(
        float rotationAmount,
        out float allowedRotation
    )
    {
        allowedRotation =
            rotationAmount;


        if (!IsHoldingBlock ||
            heldCollider == null)
        {
            return Vector3.zero;
        }


        Vector3 playerPosition =
            transform.position;


        Quaternion currentRotation =
            transform.rotation;


        Vector3 localHoldPosition =
            transform.InverseTransformPoint(
                holdPoint.position
            );


        Quaternion rotationDelta =
            Quaternion.Euler(
                0f,
                rotationAmount,
                0f
            );


        // =====================================================
        // 1. GIRO COMPLETO SIN RETROCEDER
        // =====================================================

        if (IsRotationSafe(
            playerPosition,
            currentRotation,
            rotationDelta,
            localHoldPosition
        ))
        {
            return Vector3.zero;
        }


        // =====================================================
        // 2. BUSCAR RETROCESO
        // =====================================================

        Vector3 backward =
            -transform.forward;


        float correction =
            rotationCorrectionStep;


        while (
            correction <=
            maxRotationCorrection
        )
        {
            Vector3 testPlayerPosition =
                playerPosition +
                backward *
                correction;


            if (IsRotationSafe(
                testPlayerPosition,
                currentRotation,
                rotationDelta,
                localHoldPosition
            ))
            {
                return backward *
                       correction;
            }


            correction +=
                rotationCorrectionStep;
        }


        // =====================================================
        // 3. BUSCAR MAYOR ÁNGULO POSIBLE
        // =====================================================

        float sign =
            Mathf.Sign(
                rotationAmount
            );


        float requestedAngle =
            Mathf.Abs(
                rotationAmount
            );


        float safeAngle =
            FindMaximumSafeRotation(
                playerPosition,
                currentRotation,
                localHoldPosition,
                sign,
                requestedAngle
            );


        allowedRotation =
            safeAngle *
            sign;


        return Vector3.zero;
    }


    // =========================================================
    // BUSCAR MAYOR ROTACIÓN SEGURA
    // =========================================================

    private float FindMaximumSafeRotation(
        Vector3 playerPosition,
        Quaternion currentRotation,
        Vector3 localHoldPosition,
        float sign,
        float requestedAngle
    )
    {
        float low =
            0f;


        float high =
            requestedAngle;


        for (
            int i = 0;
            i < 8;
            i++
        )
        {
            float middle =
                (low + high) *
                0.5f;


            Quaternion testDelta =
                Quaternion.Euler(
                    0f,
                    middle * sign,
                    0f
                );


            if (IsRotationSafe(
                playerPosition,
                currentRotation,
                testDelta,
                localHoldPosition
            ))
            {
                low =
                    middle;
            }
            else
            {
                high =
                    middle;
            }
        }


        return low;
    }


    // =========================================================
    // COMPROBAR ROTACIÓN
    // =========================================================

    private bool IsRotationSafe(
        Vector3 playerPosition,
        Quaternion currentPlayerRotation,
        Quaternion rotationDelta,
        Vector3 localHoldPosition
    )
    {
        for (
            int i = 0;
            i <= rotationSamples;
            i++
        )
        {
            float t =
                (float)i /
                rotationSamples;


            Quaternion interpolatedRotation =
                Quaternion.Slerp(
                    Quaternion.identity,
                    rotationDelta,
                    t
                );


            Quaternion playerRotation =
                interpolatedRotation *
                currentPlayerRotation;


            Vector3 blockPosition =
                playerPosition +
                playerRotation *
                localHoldPosition;


            Quaternion blockRotation =
                interpolatedRotation *
                holdPoint.rotation;


            // =================================================
            // ESTRUCTURAS
            // =================================================

            if (IsPositionBlocked(
                blockPosition,
                blockRotation
            ))
            {
                return false;
            }


            // =================================================
            // PLAYER
            // =================================================

            if (i > 0)
            {
                if (DoesBlockIntersectPlayer(
                    blockPosition,
                    blockRotation,
                    playerPosition,
                    playerRotation
                ))
                {
                    return false;
                }
            }
        }


        return true;
    }


    // =========================================================
    // COMPROBAR BLOQUE CONTRA PLAYER
    // =========================================================

    private bool DoesBlockIntersectPlayer(
        Vector3 blockPosition,
        Quaternion blockRotation,
        Vector3 playerPosition,
        Quaternion playerRotation
    )
    {
        Collider playerCollider =
            GetComponent<Collider>();


        if (playerCollider == null)
        {
            return false;
        }


        bool penetrating =
            Physics.ComputePenetration(
                heldCollider,
                blockPosition,
                blockRotation,

                playerCollider,
                playerPosition,
                playerRotation,

                out Vector3 direction,
                out float distance
            );


        if (!penetrating)
        {
            return false;
        }


        const float minimumPenetration =
            0.01f;


        return distance >
               minimumPenetration;
    }
}
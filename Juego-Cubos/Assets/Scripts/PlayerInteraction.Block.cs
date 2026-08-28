using UnityEngine;

public partial class PlayerInteraction
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
            if (hit.collider.gameObject ==
                heldObject)
            {
                continue;
            }


            if (hit.normal.y > 0.5f)
            {
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
                if (hit.collider.gameObject ==
                    heldObject)
                {
                    continue;
                }


                if (hit.normal.y > 0.5f)
                {
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
            if (collider.gameObject ==
                heldObject)
            {
                continue;
            }


            Rigidbody otherRigidbody =
                collider.attachedRigidbody;


            if (otherRigidbody == null ||
                otherRigidbody.isKinematic)
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


            otherRigidbody.AddForce(
                pushDirection *
                force,
                ForceMode.Force
            );
        }
    }


    // =========================================================
    // AGARRAR
    // =========================================================

    private void GrabObject(
        GameObject objectToGrab
    )
    {
        heldObject =
            objectToGrab;


        heldRigidbody =
            heldObject.GetComponent<Rigidbody>();


        heldCollider =
            heldObject.GetComponent<BoxCollider>();


        originalLayer =
            heldObject.layer;


        heldObject.layer =
            LayerMask.NameToLayer(
                "HeldFruitBlock"
            );


        if (heldRigidbody != null)
        {
            heldRigidbody.useGravity =
                false;


            heldRigidbody.isKinematic =
                true;


            heldRigidbody.position =
                holdPoint.position;


            heldRigidbody.rotation =
                holdPoint.rotation;
        }
    }


    // =========================================================
    // SOLTAR
    // =========================================================

    private void DropObject()
    {
        if (heldObject == null)
        {
            return;
        }


        heldObject.layer =
            originalLayer;


        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic =
                false;


            heldRigidbody.useGravity =
                true;
        }


        heldObject =
            null;


        heldRigidbody =
            null;


        heldCollider =
            null;
    }
}
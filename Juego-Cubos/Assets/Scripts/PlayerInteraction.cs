using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerInteraction : MonoBehaviour
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


    // =========================================================
    // ESTADO DEL BLOQUE
    // =========================================================

    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private BoxCollider heldCollider;

    private int originalLayer;


    // =========================================================
    // PROPIEDAD PÚBLICA
    // =========================================================

    public bool IsHoldingBlock =>
        heldObject != null;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // SI ESTÁ SOSTENIENDO UN BLOQUE
        // -----------------------------------------------------

        if (heldObject != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                DropObject();
            }

            return;
        }


        // -----------------------------------------------------
        // BUSCAR FRUITBLOCK
        // -----------------------------------------------------

        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            5f,
            fruitBlockLayer
        ))
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                GrabObject(
                    hit.collider.gameObject
                );
            }
        }
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (heldObject == null ||
            heldRigidbody == null ||
            heldCollider == null)
        {
            return;
        }


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
                DropObject();
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
}
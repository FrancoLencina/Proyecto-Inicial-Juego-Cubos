using UnityEngine;

public class LocalPlayerCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Camera")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;

    [Header("Camera Tilt")]
    [SerializeField] private float tilt = 20f;

    [Header("Position Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.12f;

    [Header("Rotation Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.12f;

    [Header("Look At")]
    [SerializeField] private float lookHeight = 1.5f;


    // =========================================================
    // INTERNAL
    // =========================================================

    private Vector3 positionVelocity;

    private Vector3 smoothedForward;

    private bool initialized;


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        UpdateCamera();
    }


    // =========================================================
    // ACTUALIZAR CÁMARA
    // =========================================================

    private void UpdateCamera()
    {
        // =====================================================
        // DIRECCIÓN DEL JUGADOR
        // =====================================================

        Vector3 targetForward =
            player.forward;

        targetForward.y = 0f;

        if (targetForward.sqrMagnitude < 0.0001f)
        {
            targetForward = Vector3.forward;
        }

        targetForward.Normalize();


        // =====================================================
        // INICIALIZACIÓN
        // =====================================================

        if (!initialized)
        {
            smoothedForward =
                targetForward;

            initialized = true;
        }


        // =====================================================
        // SUAVIZAR ROTACIÓN HORIZONTAL
        // =====================================================

        float rotationLerp =
            1f -
            Mathf.Exp(
                -Time.deltaTime /
                rotationSmoothTime
            );

        smoothedForward =
            Vector3.Slerp(
                smoothedForward,
                targetForward,
                rotationLerp
            );

        smoothedForward.Normalize();


        // =====================================================
        // POSICIÓN DESEADA
        // =====================================================

        Vector3 targetPosition =
            player.position
            - smoothedForward * distance
            + Vector3.up * height;


        // =====================================================
        // SEGUIMIENTO SUAVE
        // =====================================================

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref positionVelocity,
                positionSmoothTime
            );


        // =====================================================
        // PUNTO AL QUE MIRA LA CÁMARA
        // =====================================================

        Vector3 lookPosition =
            player.position +
            Vector3.up * lookHeight;


        Vector3 lookDirection =
            lookPosition -
            transform.position;


        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            // =================================================
            // ROTACIÓN BASE
            // =================================================

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up
                );


            // =================================================
            // APLICAR INCLINACIÓN
            // =================================================
            //
            // La inclinación se aplica sobre el eje X.
            //
            // Un valor positivo hace que la cámara mire
            // más hacia abajo.
            //
            // =================================================

            Quaternion tiltRotation =
                Quaternion.Euler(
                    tilt,
                    0f,
                    0f
                );


            targetRotation =
                targetRotation *
                tiltRotation;


            // =================================================
            // ROTACIÓN FINAL SUAVE
            // =================================================

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationLerp
                );
        }
    }


    // =========================================================
    // ASIGNAR PLAYER
    // =========================================================

    public void SetPlayer(
        Transform newPlayer
    )
    {
        player = newPlayer;

        initialized = false;

        positionVelocity =
            Vector3.zero;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public Transform player;

    [Header("Camera")]
    public float distance = 5f;
    public float height = 2f;
    public float verticalAngle = 15f;

    [Header("Mouse")]
    public float sensitivity = 2f;

    [Header("Vertical Rotation")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;

    private float currentVerticalRotation;

    private PlayerMovement playerMovement;
    private NetworkPlayerMovement networkPlayerMovement;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentVerticalRotation =
            verticalAngle;

        if (player != null)
        {
            SetPlayer(player);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void LateUpdate()
    {
        if (Mouse.current == null ||
            player == null)
        {
            return;
        }


        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();


        // =====================================================
        // ROTACIÓN HORIZONTAL
        // =====================================================

        float horizontalRotation =
            mouseDelta.x * sensitivity;


        if (Mathf.Abs(horizontalRotation) >
            0.0001f)
        {
            // SINGLEPLAYER
            if (playerMovement != null)
            {
                playerMovement.RequestRotation(
                    horizontalRotation
                );
            }

            // MULTIPLAYER
            if (networkPlayerMovement != null)
            {
                networkPlayerMovement.RequestRotation(
                    horizontalRotation
                );
            }
        }


        // =====================================================
        // ROTACIÓN VERTICAL DE LA CÁMARA
        // =====================================================

        currentVerticalRotation -=
            mouseDelta.y * sensitivity;


        currentVerticalRotation =
            Mathf.Clamp(
                currentVerticalRotation,
                minVerticalAngle,
                maxVerticalAngle
            );


        UpdateCameraPosition();
    }


    // =========================================================
    // ACTUALIZAR POSICIÓN DE CÁMARA
    // =========================================================

    void UpdateCameraPosition()
    {
        Quaternion verticalRotation =
            Quaternion.Euler(
                currentVerticalRotation,
                0f,
                0f
            );


        Vector3 offset =
            player.rotation *
            verticalRotation *
            new Vector3(
                0f,
                0f,
                -distance
            );


        transform.position =
            player.position +
            Vector3.up * height +
            offset;


        transform.LookAt(
            player.position +
            Vector3.up * height
        );
    }


    // =========================================================
    // ASIGNAR PLAYER
    // =========================================================

    public void SetPlayer(Transform newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogWarning(
                "PlayerCamera: se intentó asignar un Player nulo."
            );

            return;
        }


        player = newPlayer;


        playerMovement =
            player.GetComponent<PlayerMovement>();


        networkPlayerMovement =
            player.GetComponent<NetworkPlayerMovement>();


        Debug.Log(
            "PlayerCamera: Player asignado correctamente."
        );
    }
}
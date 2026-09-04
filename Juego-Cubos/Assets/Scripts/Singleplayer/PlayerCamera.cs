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

    // Indica si la cámara puede recibir input del mouse.
    private bool controlsEnabled = true;


    // =========================================================
    // START
    // =========================================================

void Start()
{
    currentVerticalRotation = verticalAngle;

    if (player != null)
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetPlayer(player);
    }
    else
    {
        // Esta cámara no está controlando a ningún jugador.
        // Dejamos el cursor libre para el menú.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

    // =========================================================
    // UPDATE
    // =========================================================

    void LateUpdate()
    {
        if (!controlsEnabled)
        {
            return;
        }

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
        // ROTACIÓN VERTICAL
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
        if (player == null)
        {
            return;
        }

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
    // ACTIVAR / DESACTIVAR CONTROLES
    // =========================================================

    public void SetControlsEnabled(
        bool enabled
    )
    {
        controlsEnabled = enabled;

        if (!enabled)
        {
            /*
             * Liberamos el cursor cuando se desactiva
             * el control de cámara.
             */
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
        else
        {
            /*
             * Volvemos al comportamiento normal del juego.
             */
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
    }


    // =========================================================
    // ASIGNAR PLAYER
    // =========================================================

    public void SetPlayer(
        Transform newPlayer
    )
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

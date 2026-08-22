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

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentVerticalRotation = verticalAngle;
    }

    void LateUpdate()
    {
        if (Mouse.current == null || player == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Rotación horizontal del personaje
        player.Rotate(
            Vector3.up * mouseDelta.x * sensitivity
        );

        // Rotación vertical de la cámara
        currentVerticalRotation -= mouseDelta.y * sensitivity;

        currentVerticalRotation = Mathf.Clamp(
            currentVerticalRotation,
            minVerticalAngle,
            maxVerticalAngle
        );

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        // Inclinación vertical de la cámara
        Quaternion verticalRotation = Quaternion.Euler(
            currentVerticalRotation,
            0f,
            0f
        );

        // Distancia de la cámara respecto al jugador
        Vector3 offset =
            player.rotation *
            verticalRotation *
            new Vector3(0f, 0f, -distance);

        // Posición final
        transform.position =
            player.position +
            Vector3.up * height +
            offset;

        // La cámara mira hacia el jugador
        transform.LookAt(
            player.position +
            Vector3.up * height
        );
    }
}
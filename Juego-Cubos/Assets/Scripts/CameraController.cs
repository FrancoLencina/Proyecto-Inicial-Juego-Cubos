using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform player;

    [Header("Camera Settings")]
    public float sensitivity = 2f;
    public float distance = 5f;
    public float height = 2f;

    [Header("Vertical Limits")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;

    private float horizontalRotation = 0f;
    private float verticalRotation = 15f;

    void LateUpdate()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        horizontalRotation += mouseDelta.x * sensitivity;
        verticalRotation -= mouseDelta.y * sensitivity;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            minVerticalAngle,
            maxVerticalAngle
        );

        Quaternion rotation = Quaternion.Euler(
            verticalRotation,
            horizontalRotation,
            0f
        );

        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

        transform.position = player.position + Vector3.up * height + offset;

        transform.LookAt(player.position + Vector3.up * height);
    }
}
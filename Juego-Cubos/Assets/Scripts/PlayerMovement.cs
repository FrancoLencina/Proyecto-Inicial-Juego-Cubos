using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Jump")]
    public float jumpForce = 5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckHeight = 1f;
    public float groundCheckRadius = 0.25f;
    public float groundCheckDistance = 0.5f;

    private Rigidbody rb;
    private Vector3 movement;

    private bool isGrounded;

    // Normal de la pared que estamos tocando
    private Vector3 wallNormal;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    void Update()
    {
        // =========================
        // GROUND CHECK
        // =========================

        CheckGround();


        // =========================
        // MOVIMIENTO
        // =========================

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal = -1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal = 1f;

        if (Keyboard.current.wKey.isPressed)
            vertical = 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical = -1f;


        movement =
            transform.right * horizontal +
            transform.forward * vertical;


        if (movement.magnitude > 1f)
            movement.Normalize();


        // =========================
        // SALTO
        // =========================

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;
        }
    }


    void FixedUpdate()
    {
        // Movimiento que vamos a aplicar
        Vector3 finalMovement = movement;


        // =========================
        // EVITAR PEGARSE A PAREDES
        // =========================

        if (wallNormal != Vector3.zero)
        {
            finalMovement = Vector3.ProjectOnPlane(
                finalMovement,
                wallNormal
            );
        }


        // =========================
        // MOVIMIENTO FÍSICO
        // =========================

        Vector3 movementAmount =
            finalMovement *
            speed *
            Time.fixedDeltaTime;

        rb.MovePosition(
            rb.position + movementAmount
        );


        // Limpiamos la normal para el siguiente FixedUpdate
        wallNormal = Vector3.zero;
    }


    // =====================================================
    // DETECCIÓN DEL SUELO
    // =====================================================

    void CheckGround()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * groundCheckHeight;

        RaycastHit hit;


        bool detected = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );


        // La superficie debe ser suficientemente horizontal.
        // Una pared tiene aproximadamente normal.y = 0.
        // Un suelo tiene normal.y = 1.

        if (detected && hit.normal.y > 0.5f)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }


    // =====================================================
    // DETECCIÓN DE PAREDES
    // =====================================================

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // Si la normal tiene poco componente Y,
            // estamos tocando una superficie vertical/inclinada.

            if (contact.normal.y < 0.5f)
            {
                wallNormal = contact.normal;
                break;
            }
        }
    }


    // =====================================================
    // VISUALIZACIÓN DEL SPHERECAST
    // =====================================================

    void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * groundCheckHeight;


        RaycastHit hit;


        bool detected = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );


        if (detected && hit.normal.y > 0.5f)
        {
            Gizmos.color = Color.green;
        }
        else if (detected)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.red;
        }


        // Esfera inicial
        Gizmos.DrawWireSphere(
            origin,
            groundCheckRadius
        );


        // Esfera final
        Vector3 end =
            origin +
            Vector3.down *
            groundCheckDistance;


        Gizmos.DrawWireSphere(
            end,
            groundCheckRadius
        );


        // Recorrido
        Gizmos.DrawLine(
            origin,
            end
        );
    }
}
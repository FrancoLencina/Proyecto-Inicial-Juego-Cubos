using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fruitBlockLayer;
    [SerializeField] private Transform holdPoint;

    private GameObject heldObject;
    private Rigidbody heldRigidbody;

    private int originalLayer;

    private Vector3 collisionNormal;
    private bool isColliding;
    private HeldBlockCollision heldBlockCollision;

    private void Update()
    {
        if (heldObject != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                DropObject();
            }

            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, fruitBlockLayer))
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                GrabObject(hit.collider.gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        if (heldObject == null || heldRigidbody == null)
            return;

        Vector3 difference = holdPoint.position - heldRigidbody.position;

        heldRigidbody.linearVelocity =
            difference / Time.fixedDeltaTime;

        heldRigidbody.MoveRotation(holdPoint.rotation);
    }

    private void GrabObject(GameObject objectToGrab)
    {
        heldObject = objectToGrab;

        heldRigidbody = heldObject.GetComponent<Rigidbody>();
        heldBlockCollision = heldObject.GetComponent<HeldBlockCollision>();

        originalLayer = heldObject.layer;
        heldObject.layer = LayerMask.NameToLayer("HeldFruitBlock");

        if (heldRigidbody != null)
        {
            heldRigidbody.useGravity = false;
            heldRigidbody.isKinematic = false;
        }
    }

    private void DropObject()
    {
        if (heldObject == null)
            return;

        heldObject.layer = originalLayer;

        if (heldRigidbody != null)
        {
            heldRigidbody.useGravity = true;
        }

        heldObject = null;
        heldRigidbody = null;
        heldBlockCollision = null;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.contactCount == 0)
            return;

        isColliding = true;

        Vector3 normal = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            normal += collision.GetContact(i).normal;
        }

        collisionNormal = normal.normalized;

        Debug.Log(
            "FruitBlock colisionando con: "
            + collision.gameObject.name
            + " | Normal: "
            + collisionNormal
        );
    }

    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        collisionNormal = Vector3.zero;
    }
}
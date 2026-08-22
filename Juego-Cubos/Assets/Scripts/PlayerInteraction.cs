using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fruitBlockLayer;
    [SerializeField] private Transform holdPoint;

    private GameObject heldObject;

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
            //Debug.Log("Estoy mirando un FruitBlock: " + hit.collider.gameObject.name);

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                GrabObject(hit.collider.gameObject);
            }
        }
    }

    private void GrabObject(GameObject objectToGrab)
    {
        heldObject = objectToGrab;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        Collider col = heldObject.GetComponent<Collider>();

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        heldObject.transform.position = holdPoint.position;
        heldObject.transform.rotation = holdPoint.rotation;

        heldObject.transform.SetParent(holdPoint);
    }

    private void DropObject()
    {
        heldObject.transform.SetParent(null);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        Collider col = heldObject.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        heldObject = null;
    }
}
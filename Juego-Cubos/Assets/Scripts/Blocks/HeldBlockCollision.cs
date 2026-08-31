using UnityEngine;

public class HeldBlockCollision : MonoBehaviour
{
    public bool IsBlocked { get; private set; }
    public Vector3 CollisionNormal { get; private set; }

    private void FixedUpdate()
    {
        IsBlocked = false;
        CollisionNormal = Vector3.zero;
    }

    private void OnCollisionStay(Collision collision)
    {
        // Ignorar otros FruitBlocks.
        if (collision.gameObject.layer == LayerMask.NameToLayer("FruitBlocks"))
            return;

        // Ignorar al Player por las Layers.
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            return;

        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;

            // Consideramos bloqueo solamente contra superficies
            // aproximadamente verticales.
            if (Mathf.Abs(normal.y) < 0.5f)
            {
                IsBlocked = true;
                CollisionNormal = normal;
            }
        }
    }
}
using UnityEngine;

public class HeldBlockCollision : MonoBehaviour
{
    public Vector3 CollisionNormal { get; private set; }
    public bool IsColliding { get; private set; }

    private void FixedUpdate()
    {
        IsColliding = false;
        CollisionNormal = Vector3.zero;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.contactCount == 0)
            return;

        IsColliding = true;

        Vector3 normal = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            normal += collision.GetContact(i).normal;
        }

        CollisionNormal = normal.normalized;
    }
}

using UnityEngine;

public class SphereCastVisualizer : MonoBehaviour
{
    public float radius = 0.25f;
    public float distance = 5f;
    public LayerMask groundLayer;

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;

        RaycastHit hit;

        bool detected = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out hit,
            distance,
            groundLayer
        );

        bool validGround = false;

        if (detected)
        {
            // 1 = piso horizontal
            // 0 = pared vertical
            validGround = hit.normal.y > 0.5f;
        }

        // Verde = suelo válido
        // Amarillo = detectó algo pero no es suelo
        // Rojo = no detectó nada

        if (validGround)
            Gizmos.color = Color.green;
        else if (detected)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(origin, radius);

        Vector3 end = origin + Vector3.down * distance;

        Gizmos.DrawWireSphere(end, radius);

        Gizmos.DrawLine(origin, end);

        if (detected)
        {
            Debug.Log(
                "Detectó: " +
                hit.collider.gameObject.name +
                " | Normal Y: " +
                hit.normal.y
            );

            Gizmos.color = Color.blue;

            Gizmos.DrawLine(
                hit.point,
                hit.point + hit.normal
            );
        }
    }
}
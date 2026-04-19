using UnityEngine;

/// <summary>
/// Draws a visible gizmo in the Scene view for avatar spawn points.
/// Shows a colored wireframe body, forward direction arrow, ground circle, and label.
/// Attach to empty GameObjects that mark spawn locations.
/// </summary>
public class SpawnPointGizmo : MonoBehaviour
{
    [HideInInspector] public Color gizmoColor = Color.green;
    [HideInInspector] public string label = "Spawn";

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Vector3 basePos = transform.position;
        Vector3 bodyCenter = basePos + Vector3.up * 0.9f;
        float radius = 0.25f;

        // Wireframe body
        Gizmos.DrawWireSphere(bodyCenter + Vector3.up * 0.65f, radius);       // Head
        Gizmos.DrawWireSphere(bodyCenter, radius * 0.9f);                      // Torso
        Gizmos.DrawWireSphere(bodyCenter - Vector3.up * 0.5f, radius * 0.7f); // Hips
        Gizmos.DrawLine(bodyCenter + Vector3.up * 0.65f, bodyCenter);
        Gizmos.DrawLine(bodyCenter, bodyCenter - Vector3.up * 0.5f);

        // Forward direction arrow
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Vector3 fwd = transform.forward;
        Vector3 arrowStart = bodyCenter;
        Vector3 arrowEnd = arrowStart + fwd * 1.2f;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        Vector3 right = transform.right;
        Gizmos.DrawLine(arrowEnd, arrowEnd - fwd * 0.25f + right * 0.15f);
        Gizmos.DrawLine(arrowEnd, arrowEnd - fwd * 0.25f - right * 0.15f);

        // Ground circle
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.4f);
        DrawWireCircle(basePos + Vector3.up * 0.01f, 0.5f, 24);

        // Label
        GUIStyle style = new GUIStyle();
        style.normal.textColor = gizmoColor;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 14;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(bodyCenter + Vector3.up * 1.2f, label, style);
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
#endif
}

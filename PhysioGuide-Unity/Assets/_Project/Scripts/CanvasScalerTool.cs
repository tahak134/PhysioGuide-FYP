using UnityEngine;

[ExecuteInEditMode]
public class CanvasScalerTool : MonoBehaviour
{
    public float targetScale = 0.001f;

    [ContextMenu("Scale Canvas Correctly")]
    public void ScaleCanvas()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;

        // Calculate the ratio to keep the physical size consistent
        float ratio = rect.localScale.x / targetScale;

        // Apply new dimensions and scale
        rect.sizeDelta = new Vector2(rect.sizeDelta.x * ratio, rect.sizeDelta.y * ratio);
        rect.localScale = new Vector3(targetScale, targetScale, targetScale);

        Debug.Log("Canvas Scaled. Press 'F' in Scene View to find it!");
    }
}
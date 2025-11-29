using UnityEngine;

public class CalibrationManager : MonoBehaviour
{
    public float ScaleFactor = 1.0f; // Default
    public Vector3 RootOffset = Vector3.zero;

    // Call this when user is in T-Pose
    public void Calibrate(PoseData currentData)
    {
        // 1. Get Wrist positions from JSON data
        Vector3 leftWrist = new Vector3(currentData.left_wrist.x, currentData.left_wrist.y, 0);
        Vector3 rightWrist = new Vector3(currentData.right_wrist.x, currentData.right_wrist.y, 0);

        // 2. Calculate Normalized Distance
        float distMP = Vector3.Distance(leftWrist, rightWrist);

        // 3. Assume Average Human Span (1.6 meters)
        float distReal = 1.6f;

        // 4. Set Scale Factor
        ScaleFactor = distReal / distMP;

        UnityEngine.Debug.Log($"Calibrated! Scale Factor: {ScaleFactor}");
    }

    // Helper function for other scripts to use
    public Vector3 GetWorldPosition(Vector3 mediaPipePos)
    {
        float x = (mediaPipePos.x - 0.5f) * ScaleFactor;
        float y = (1.0f - mediaPipePos.y) * ScaleFactor; // Invert Y
        float z = mediaPipePos.z * ScaleFactor;
        return new Vector3(x, y, z) + RootOffset;
    }
}
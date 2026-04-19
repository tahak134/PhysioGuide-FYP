using UnityEngine;

public class CalibrationManager : MonoBehaviour
{
    public float ScaleFactor = 1.0f;
    public Vector3 RootOffset = Vector3.zero;
    public bool IsCalibrated { get; private set; } = false;

    [Header("Calibration Settings")]
    [Tooltip("Minimum wrist distance in MediaPipe units to accept a T-pose")]
    public float minWristDistance = 0.3f;

    /// <summary>
    /// Attempts calibration from a live PoseData frame.
    /// Returns true if calibration succeeded (user was in valid T-pose).
    /// </summary>
    public bool TryCalibrate(PoseData currentData)
    {
        if (currentData == null || currentData.raw_landmarks == null || currentData.raw_landmarks.Count <= 16)
            return false;

        Landmark lWrist = currentData.raw_landmarks[15];
        Landmark rWrist = currentData.raw_landmarks[16];

        Vector3 leftWrist = new Vector3(lWrist.x, lWrist.y, 0);
        Vector3 rightWrist = new Vector3(rWrist.x, rWrist.y, 0);

        float distMP = Vector3.Distance(leftWrist, rightWrist);

        // Reject if wrists are too close (not a valid T-pose)
        if (distMP < minWristDistance)
            return false;

        // Check if arms are roughly horizontal (wrist Y values should be similar)
        float yDiff = Mathf.Abs(lWrist.y - rWrist.y);
        if (yDiff > 0.15f) // Too much vertical difference — not a T-pose
            return false;

        float distReal = 1.6f; // Average human arm span
        ScaleFactor = distReal / distMP;
        IsCalibrated = true;
        Debug.Log($"[Calibration] Success! Scale Factor: {ScaleFactor:F3}");
        return true;
    }

    /// <summary>
    /// Original calibration method (for direct calls).
    /// </summary>
    public void Calibrate(PoseData currentData)
    {
        if (currentData.raw_landmarks == null || currentData.raw_landmarks.Count <= 16) return;

        Landmark lWrist = currentData.raw_landmarks[15];
        Landmark rWrist = currentData.raw_landmarks[16];

        Vector3 leftWrist = new Vector3(lWrist.x, lWrist.y, 0);
        Vector3 rightWrist = new Vector3(rWrist.x, rWrist.y, 0);

        float distMP = Vector3.Distance(leftWrist, rightWrist);

        float distReal = 1.6f;
        if (distMP > 0.1f)
        {
            ScaleFactor = distReal / distMP;
            IsCalibrated = true;
            Debug.Log($"[Calibration] Calibrated! Scale Factor: {ScaleFactor}");
        }
    }

    public void ResetCalibration()
    {
        ScaleFactor = 1.0f;
        RootOffset = Vector3.zero;
        IsCalibrated = false;
    }

    public Vector3 GetWorldPosition(Landmark mediaPipePos)
    {
        float x = (mediaPipePos.x - 0.5f) * ScaleFactor;
        float y = (1.0f - mediaPipePos.y) * ScaleFactor;
        float z = mediaPipePos.z * ScaleFactor;

        return new Vector3(x, y, z) + RootOffset;
    }
}
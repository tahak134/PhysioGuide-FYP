using UnityEngine;

public class AvatarController : MonoBehaviour
{
    [Header("Bone References")]
    public Transform bone_LeftUpperArm;
    public Transform bone_RightUpperArm;
    public Transform bone_LeftLowerArm;
    public Transform bone_RightLowerArm;
    public Transform bone_LeftShoulder;
    public Transform bone_RightShoulder;
    public Transform bone_Neck;

    [Header("Scaption Settings (PRESERVED)")]
    public float smoothSpeed = 10f;
    public bool invertLift = false;
    public float frontOffsetLeft = -90f;
    public float frontOffsetRight = 90f;
    [Range(0, 2)] public int liftAxis = 0; // 0=X, 1=Y, 2=Z

    [Header("Chin Tuck Settings (NEW)")]
    public bool invertNeck = false;   // Check this if he looks UP instead of DOWN
    public float neckOffset = 0f;     // Adjust this to fix starting head position
    public float armsDownAngle = 80f;
    public bool invertDown = true;
    [Range(0, 2)] public int downAxis = 0;

    // --- INTERNAL MEMORY ---
    private Quaternion _startRot_LeftUpper;
    private Quaternion _startRot_RightUpper;
    private Quaternion _startRot_LeftLower;
    private Quaternion _startRot_RightLower;
    private Quaternion _startRot_LeftShoulder;
    private Quaternion _startRot_RightShoulder;
    private Quaternion _startRot_Neck;

    void Start()
    {
        if (bone_LeftUpperArm) _startRot_LeftUpper = bone_LeftUpperArm.localRotation;
        if (bone_RightUpperArm) _startRot_RightUpper = bone_RightUpperArm.localRotation;
        if (bone_LeftLowerArm) _startRot_LeftLower = bone_LeftLowerArm.localRotation;
        if (bone_RightLowerArm) _startRot_RightLower = bone_RightLowerArm.localRotation;
        if (bone_LeftShoulder) _startRot_LeftShoulder = bone_LeftShoulder.localRotation;
        if (bone_RightShoulder) _startRot_RightShoulder = bone_RightShoulder.localRotation;
        if (bone_Neck) _startRot_Neck = bone_Neck.localRotation;
    }

    public void UpdatePose(PoseData data)
    {
        if (data == null || data.metrics == null) return;

        bool isChinTuck = (data.exercise == "chin_tuck");

        if (isChinTuck)
        {
            PerformChinTuck(data);
        }
        else
        {
            PerformScaption(data);
        }
    }

    void PerformScaption(PoseData data)
    {
        float liftAngle = data.metrics.SEA; // Raw angle
        liftAngle -= 90f; // (Optional: restore if needed based on previous steps)

        if (invertLift) liftAngle = -liftAngle;
        float bendAngle = 180f - data.metrics.EA;

        Quaternion liftRot = Quaternion.identity;
        switch (liftAxis)
        {
            case 0: liftRot = Quaternion.Euler(liftAngle, 0, 0); break;
            case 1: liftRot = Quaternion.Euler(0, liftAngle, 0); break;
            case 2: liftRot = Quaternion.Euler(0, 0, liftAngle); break;
        }

        if (bone_LeftUpperArm != null)
        {
            Quaternion toFront = Quaternion.Euler(0, 0, frontOffsetLeft);
            Quaternion target = _startRot_LeftUpper * toFront * liftRot;
            bone_LeftUpperArm.localRotation = Quaternion.Slerp(bone_LeftUpperArm.localRotation, target, Time.deltaTime * smoothSpeed);
        }

        if (bone_RightUpperArm != null)
        {
            Quaternion toFront = Quaternion.Euler(0, 0, frontOffsetRight);
            Quaternion target = _startRot_RightUpper * toFront * liftRot;
            bone_RightUpperArm.localRotation = Quaternion.Slerp(bone_RightUpperArm.localRotation, target, Time.deltaTime * smoothSpeed);
        }

        // Elbows
        if (bone_LeftLowerArm) bone_LeftLowerArm.localRotation = Quaternion.Slerp(bone_LeftLowerArm.localRotation, _startRot_LeftLower * Quaternion.Euler(0, -bendAngle, 0), Time.deltaTime * smoothSpeed);
        if (bone_RightLowerArm) bone_RightLowerArm.localRotation = Quaternion.Slerp(bone_RightLowerArm.localRotation, _startRot_RightLower * Quaternion.Euler(0, bendAngle, 0), Time.deltaTime * smoothSpeed);

        // Shoulders (Reset)
        if (bone_RightShoulder) bone_RightShoulder.localRotation = Quaternion.Slerp(bone_RightShoulder.localRotation, _startRot_RightShoulder, Time.deltaTime * 5f);
        if (bone_LeftShoulder) bone_LeftShoulder.localRotation = Quaternion.Slerp(bone_LeftShoulder.localRotation, _startRot_LeftShoulder, Time.deltaTime * 5f);
    }


    // --- LOGIC FOR CHIN TUCK (NECK) ---
    void PerformChinTuck(PoseData data)
    {
        if (bone_Neck == null) return;

        // High NAM (40) = Head Forward (Bad)
        // Low NAM (10) = Chin Tucked (Good)
        float neckAngle = data.metrics.GetNAM;
        float finalAngle = (neckAngle * 870f) + neckOffset;
        
        if (invertNeck) finalAngle = -finalAngle;

        // This makes the character look Up/Down or Forward/Back
        Quaternion neckMove = Quaternion.Euler(finalAngle, 0, 0);

        // 4. Combine with Start Rotation
        bone_Neck.localRotation = Quaternion.Slerp(bone_Neck.localRotation, _startRot_Neck * neckMove, Time.deltaTime * smoothSpeed);

        Quaternion leftDown = Quaternion.Euler(armsDownAngle, 0, 0);
        Quaternion rightDown = Quaternion.Euler(armsDownAngle, 0, 0);

        // Optional: Reset arms to neutral during Chin Tuck
        if (bone_LeftUpperArm) bone_LeftUpperArm.localRotation = Quaternion.Slerp(bone_LeftUpperArm.localRotation, _startRot_LeftUpper * leftDown , Time.deltaTime * smoothSpeed);
        if (bone_RightUpperArm) bone_RightUpperArm.localRotation = Quaternion.Slerp(bone_RightUpperArm.localRotation, _startRot_RightUpper * rightDown, Time.deltaTime * smoothSpeed);
    }
}
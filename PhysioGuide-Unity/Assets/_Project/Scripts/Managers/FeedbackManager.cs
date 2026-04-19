using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    [Header("Visuals")]
    public Renderer avatarRenderer;
    public Material correctMat; // Green
    public Material errorMat;   // Red

    // This method is now called by UserPoseUpdater
    public void UpdateVisuals(PoseData pose)
    {
        // Safety Check
        if (pose.error_flags == null) return;

        // --- 1. DETECT ERRORS (Using Correct Python Keys) ---
        bool hasError = false;
        string feedbackMsg = "Good Form";

        // Logic handles both exercises based on what flags are active
        if (pose.error_flags.hiking)
        {
            hasError = true;
            feedbackMsg = "Relax Shoulders";
        }
        else if (pose.error_flags.bent_elbow)
        {
            hasError = true;
            feedbackMsg = "Straighten Arms";
        }
        else if (pose.error_flags.flexion) // Chin Tuck specific
        {
            hasError = true;
            feedbackMsg = "Chin Too High";
        }
        else if (pose.error_flags.extension) // Chin Tuck specific
        {
            hasError = true;
            feedbackMsg = "Chin Too Low";
        }

        // --- 2. UPDATE AVATAR COLOR ---
        if (avatarRenderer != null)
        {
            avatarRenderer.material = hasError ? errorMat : correctMat;
        }

        // --- 3. UPDATE UI (HUD) ---
        // Ensure UIManager has this method exposed
        if (UIManager.Instance != null)
        {
            // We pass the Message, the Error State (for text color), and Reps
            UIManager.Instance.UpdateLiveFeedback(feedbackMsg, hasError, pose.rep_count);
        }
    }
}
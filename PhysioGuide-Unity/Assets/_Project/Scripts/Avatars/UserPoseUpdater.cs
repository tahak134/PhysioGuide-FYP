using UnityEngine;

public class UserPoseUpdater : MonoBehaviour
{
    [Header("Subsystems")]
    public AvatarController userController;
    public FeedbackManager feedbackManager;

    void Update()
    {
        // 1. Safety Checks
        if (ZMQClient.Instance == null) return;
        string json = ZMQClient.Instance.LatestPoseJson;
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            // 2. Parse Once
            PoseData livePose = JsonUtility.FromJson<PoseData>(json);

            // 3. Debug (Optional, remove for production)
            if (livePose != null)
            {

                // Drive Animation
                if (userController != null) userController.UpdatePose(livePose);
                if (feedbackManager != null) feedbackManager.UpdateVisuals(livePose);

                // Log frame for session history
                if (SessionLogger.Instance != null) SessionLogger.Instance.LogFrame(livePose);
            }
        }
        catch (System.Exception e)
        {
            // JsonUtility is sensitive; catch generic errors to prevent crash

            Debug.LogWarning("Error parsing JSON: " + e.Message);
        }
    }
}

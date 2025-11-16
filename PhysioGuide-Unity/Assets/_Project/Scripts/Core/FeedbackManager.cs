using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    // We will drag the UserAvatar's body/mesh renderer here 
    public Renderer avatarRenderer;
    // Create these two materials in Unity and drag them here 
    public Material correctMaterial;
    public Material errorMaterial;
    void Update()
    {
        if (avatarRenderer == null) return;
        string liveJson = ZMQClient.incomingPoseData;
        if (string.IsNullOrEmpty(liveJson)) return;
        try
        {
            PoseData pose = JsonUtility.FromJson<PoseData>(liveJson);
            // This is where we use Fatima's rules! 
            // Check if ANY error flag from Hussain's data is true. 
            if (pose.error_hiking_l | pose.error_neck_flexion) { avatarRenderer.material = errorMaterial; }
            else
            {
                avatarRenderer.material = correctMaterial;
            }
        }
        catch
        { /* Ignore errors, UserPoseUpdater will log them */ }
    }
}
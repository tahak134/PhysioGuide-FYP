using System.Diagnostics;
using UnityEngine;

public class UserPoseUpdater : MonoBehaviour
{
    // We will drag the UserAvatar's AvatarController script here 
    public AvatarController avatarController;

    void Update()
    {
        if (avatarController == null) return;

        // Get the LIVE data string from the ZMQ script 
        string liveJson = ZMQClient.incomingPoseData;

        if (string.IsNullOrEmpty(liveJson)) return;

        try
        {
            // Convert the LIVE JSON string into our PoseData object 
            PoseData pose = JsonUtility.FromJson<PoseData>(liveJson);

            // Send the LIVE pose data to the user's puppeteer 
            avatarController.UpdatePose(pose);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning("Error parsing JSON: " + e.Message);
        }
    }
}

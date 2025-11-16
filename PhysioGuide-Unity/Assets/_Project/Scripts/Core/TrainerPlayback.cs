using UnityEngine;
using System.IO; // For reading files 
using System.Collections.Generic; // For Lists 

public class TrainerPlayback: MonoBehaviour
{ 
    // We will drag the Scaption_Perfect.json file here 
    public TextAsset poseFile;

    // We will drag the TrainerAvatar's AvatarController script here 
    public AvatarController avatarController;

    private List<string> poseDataLines = new List<string>();
    private int currentFrame = 0;

    void Start()
    {
        // Load the JSON file from the TextAsset and split it by line 
        if (poseFile != null)
        {
            poseDataLines.AddRange(poseFile.text.Split('\n'));
        }
    }

    // We use FixedUpdate for physics/animation loops 
    void FixedUpdate()
    {
        if (poseDataLines.Count == 0 | avatarController == null) return;
        // Get the JSON string for the current frame 
        string json = poseDataLines[currentFrame];

        if (!string.IsNullOrEmpty(json))
        {
            // Convert the JSON string into our PoseData object 
            PoseData pose = JsonUtility.FromJson<PoseData>(json);

            // Send the "perfect" pose data to the trainer's puppeteer 
            avatarController.UpdatePose(pose);
        }

        // Move to the next frame 
        currentFrame++;

        // Loop the "animation" 
        if (currentFrame >= poseDataLines.Count)
        {
            currentFrame = 0;
        }
    }
} 

using UnityEngine;

public class AvatarController: MonoBehaviour
{
    // We will drag our avatar's bones onto these slots in the Inspector 
    public Transform bone_LeftShoulder;
    public Transform bone_LeftElbow;
    public Transform bone_RightShoulder;
    public Transform bone_RightElbow;
    //... add public Transforms for every bone you need to move 
    // This is the "master" function. We will call this from 
    // other scripts to give this avatar its instructions. 
    public void UpdatePose(PoseData data)
    {
        if (data == null) return;
        // --- This is where the core logic happens --- 
        // We receive the "correct" angles from Fatima/Hussain's data. 
        // We need to apply them to our bones using Quaternions. 

        // This is a simplified example. The 'scaption_angle_l' 
        // will likely drive the Z axis. You will need to experiment 
        // to find the correct axis (X, Y, or Z) and add any 
        // necessary offsets (e.g., + 90 degrees) to match the model. 

        bone_LeftShoulder.localRotation =
            Quaternion.Euler(0, 0, data.scaption_angle_l);

        bone_RightShoulder.localRotation =
            Quaternion.Euler(0, 0, data.scaption_angle_r);
    }
}

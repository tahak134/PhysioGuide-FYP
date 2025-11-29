using UnityEngine;
using System; 

[System.Serializable]
public class PoseData
{
    // --- KINEMATIC DATA --- 
    public float shoulder_flexion_l;
    public float shoulder_flexion_r;
    public float elbow_flexion_l;
    public float elbow_flexion_r;
    public float spine_rotation;
    public float scaption_angle_r;
    public float scaption_angle_l;

    // This allows you to access .x and .y in your CalibrationManager
    public BodyPoint left_wrist;
    public BodyPoint right_wrist;

    // --- HEURISTIC DATA --- 
    public bool error_hiking_l;
    public bool error_hiking_r;
    public bool error_neck_flexion;
    public bool error_tempo_fast;
    public bool error_rom_low;

    // --- METADATA --- 
    public float confidence;
    public long timestamp;
}

// Define the structure for the points (x, y)
[System.Serializable]
public class BodyPoint
{
    public float x;
    public float y;
    public float z; // Optional: include Z if your Python code sends it
}
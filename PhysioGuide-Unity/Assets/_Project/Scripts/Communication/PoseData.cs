using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PoseData
{
    // Patient identification
    public string patient_id;

    // Metadata
    public long timestamp;
    public string exercise;      // e.g., "scaption"
    public string state;         // e.g., "LIFTING", "HOLDING"
    public int rep_count;        // Current repetition count
    public string status;        // e.g., "calibrating", "tracking"

    // Nested Data Objects
    public Metrics metrics;
    public ErrorFlags error_flags;
    public List<Landmark> raw_landmarks;
}

[System.Serializable]
public class Metrics
{

    public float NAM;
    public float nam;
    public float HRM;
    public float SMI;
    public float SEA;
    public float EA;
    public float SHM;
    public float GetNAM => (NAM != 0) ? NAM : nam;

}

[Serializable]
public class ErrorFlags
{
    // Compensation Flags
    public bool compensation_trunk;
    public bool compensation_elbow_flexion;
    public bool compensation_shoulder_hike;
    public bool chin_tuck_good;

    public bool flexion;    // "Chin too high"
    public bool extension;  // "Chin too low"

    // Legacy Flags
    public bool hiking;
    public bool bent_elbow;
    public bool momentum;
}

[Serializable]
public class Landmark
{
    public float x;
    public float y;
    public float z;
    public float vis; // Visibility score
}

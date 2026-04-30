using System;
using System.Collections.Generic;

/// <summary>
/// Lightweight session metadata saved alongside each session recording.
/// Designed to be uploaded to the Therapist Dashboard (Firebase Firestore).
/// </summary>
[Serializable]
public class SessionSummary
{
    public string patient_id;
    public string patient_name;
    public string exercise;
    public string timestamp;        // ISO 8601 format
    public float duration_sec;
    public int total_reps;
    public int total_frames;
    public float accuracy;          // 0-100%
    public List<string> error_flags_detected;
    public string recording_filename;
    public string recording_url;     // Firebase Storage download URL for the full recording
}

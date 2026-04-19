using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance;

    private List<PoseData> _sessionData = new List<PoseData>();
    private bool _isLogging = false;

    // Tracking stats during session
    private int _errorFrameCount = 0;
    private int _totalFrameCount = 0;
    private HashSet<string> _detectedErrors = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSessionStart += StartLogging;
            GameManager.Instance.OnSessionEnd += StopLogging;
        }
        else
        {
            Debug.LogError("CRITICAL: GameManager not found! Make sure '_AppManager' is in the scene.");
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSessionStart -= StartLogging;
            GameManager.Instance.OnSessionEnd -= StopLogging;
        }
    }

    public void StartLogging()
    {
        _sessionData.Clear();
        _errorFrameCount = 0;
        _totalFrameCount = 0;
        _detectedErrors.Clear();
        _isLogging = true;
        Debug.Log("[SessionLogger] Started.");
    }

    public void StopLogging()
    {
        if (!_isLogging) return;
        _isLogging = false;
        SaveSession();
    }

    public void LogFrame(PoseData data)
    {
        if (!_isLogging || data == null) return;

        // Stamp patient ID onto each frame
        string patientId = PlayerPrefs.GetString("PatientID", "anonymous");
        if (string.IsNullOrEmpty(data.patient_id))
        {
            data.patient_id = patientId;
        }

        _sessionData.Add(data);
        _totalFrameCount++;

        // Track errors for accuracy calculation
        if (data.error_flags != null)
        {
            bool hasError = data.error_flags.hiking || data.error_flags.bent_elbow
                         || data.error_flags.flexion || data.error_flags.extension
                         || data.error_flags.compensation_trunk
                         || data.error_flags.compensation_elbow_flexion
                         || data.error_flags.compensation_shoulder_hike;

            if (hasError) _errorFrameCount++;

            if (data.error_flags.hiking) _detectedErrors.Add("hiking");
            if (data.error_flags.bent_elbow) _detectedErrors.Add("bent_elbow");
            if (data.error_flags.flexion) _detectedErrors.Add("flexion");
            if (data.error_flags.extension) _detectedErrors.Add("extension");
            if (data.error_flags.compensation_trunk) _detectedErrors.Add("compensation_trunk");
            if (data.error_flags.compensation_elbow_flexion) _detectedErrors.Add("compensation_elbow_flexion");
            if (data.error_flags.compensation_shoulder_hike) _detectedErrors.Add("compensation_shoulder_hike");
        }
    }

    /// <summary>
    /// Returns the computed accuracy (0-100) based on error frames vs total frames.
    /// </summary>
    public float GetSessionAccuracy()
    {
        if (_totalFrameCount == 0) return 100f;
        return (1f - (float)_errorFrameCount / _totalFrameCount) * 100f;
    }

    /// <summary>
    /// Returns the last session summary after StopLogging is called.
    /// Used by UIManager to populate the Summary screen.
    /// </summary>
    public SessionSummary LastSessionSummary { get; private set; }

        private void SaveSession()
    {
        string patientId = PlayerPrefs.GetString("PatientID", "anonymous");
        string patientName = PlayerPrefs.GetString("PatientName", "Unknown Patient");
        string exercise = PlayerPrefs.GetString("SelectedExercise", "unknown");

        string folderPath = Path.Combine(Application.persistentDataPath, "Sessions", patientId);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string dateStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string recordingFilename = "Session_" + exercise + "_" + dateStamp + ".json";
        string fullPath = Path.Combine(folderPath, recordingFilename);

        string json = "[]";
        if (_sessionData.Count > 0)
        {
            json = JsonHelper.ToJson(_sessionData.ToArray(), true);
            File.WriteAllText(fullPath, json);
            Debug.Log("[SessionLogger] Recording saved to: " + fullPath);
        }
        else
        {
            Debug.LogWarning("[SessionLogger] No frame data to save to json.");
        }

        float accuracy = GetSessionAccuracy();
        int maxReps = 0;
        foreach (var frame in _sessionData)
        {
            if (frame.rep_count > maxReps) maxReps = frame.rep_count;
        }

        LastSessionSummary = new SessionSummary
        {
            patient_id = patientId,
            patient_name = patientName,
            exercise = exercise,
            timestamp = System.DateTime.Now.ToString("o"),
            duration_sec = GameManager.Instance != null ? GameManager.Instance.SessionDuration : 0f,
            total_reps = maxReps,
            total_frames = _totalFrameCount,
            accuracy = accuracy,
            error_flags_detected = new List<string>(_detectedErrors),
            recording_filename = recordingFilename
        };

        string summaryFilename = "Summary_" + exercise + "_" + dateStamp + ".json";
        string summaryPath = Path.Combine(folderPath, summaryFilename);
        string summaryJson = JsonUtility.ToJson(LastSessionSummary, true);
        File.WriteAllText(summaryPath, summaryJson);
        Debug.Log("[SessionLogger] Summary saved to: " + summaryPath);

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.UploadSessionData(LastSessionSummary, json);
        }
    }
}

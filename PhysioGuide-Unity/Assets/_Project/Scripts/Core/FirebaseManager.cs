using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles Firestore operations via REST APIs.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    [Header("Firebase Configuration")]
    [Tooltip("Firebase Web API Key from Project Settings")]
    public string apiKey = "YOUR_FIREBASE_API_KEY";
    [Tooltip("Firebase Project ID")]
    public string projectId = "YOUR_PROJECT_ID";

    // Patient Data (Cached)
    public string PatientId { get; private set; } = "";
    public string PatientName { get; private set; } = "";
    public bool IsAuthenticated => !string.IsNullOrEmpty(PatientId);

    // Firestore endpoint
    private string FirestoreBaseUrl => $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Restore state from PlayerPrefs
        PatientId = PlayerPrefs.GetString("PatientID", "");
        PatientName = PlayerPrefs.GetString("PatientName", "");
    }

    public void VerifyPatientId(string rawKey, Action<bool, string, string> callback)
    {
        StartCoroutine(VerifyPatientIdCoroutine(rawKey, callback));
    }

    private IEnumerator VerifyPatientIdCoroutine(string rawKey, Action<bool, string, string> callback)
    {
        // The dashboard creates patient documents with ID "pat_{KEY}"
        // Users enter just the 6-character key, so we add the prefix.
        string docId = rawKey.StartsWith("pat_") ? rawKey : $"pat_{rawKey}";
        string url = $"{FirestoreBaseUrl}/patients/{docId}?key={apiKey}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Patient found in Firestore
                string responseBody = request.downloadHandler.text;
                string pName = "Patient " + docId;
                
                // Dashboard stores the name in "full_name" field
                if (responseBody.Contains("\"full_name\""))
                {
                    try {
                        string nameMarker = "\"full_name\":";
                        int idx = responseBody.IndexOf(nameMarker);
                        if (idx > 0)
                        {
                            string sub = responseBody.Substring(idx + nameMarker.Length);
                            int stringValIdx = sub.IndexOf("\"stringValue\":");
                            if (stringValIdx > 0)
                            {
                                int startQuote = sub.IndexOf("\"", stringValIdx + 14);
                                int endQuote = sub.IndexOf("\"", startQuote + 1);
                                if (startQuote > 0 && endQuote > startQuote)
                                    pName = sub.Substring(startQuote + 1, endQuote - startQuote - 1);
                            }
                        }
                    } catch {}
                }
                
                Debug.Log($"[Firebase] Verified Patient: {pName} (doc: {docId})");
                PatientId = docId;  // Store the full doc ID (pat_XXXXXX)
                PatientName = pName;
                PlayerPrefs.SetString("PatientID", PatientId);
                PlayerPrefs.SetString("PatientName", PatientName);
                PlayerPrefs.Save();
                
                callback?.Invoke(true, docId, pName);
            }
            else
            {
                Debug.LogWarning($"[Firebase] Patient verification failed for '{docId}': {request.downloadHandler.text}");
                callback?.Invoke(false, "", "Patient ID not found. Please check your key.");
            }
        }
    }

    public void UploadSessionData(SessionSummary summary, string recordingDataJson)
    {
        StartCoroutine(UploadSessionDataCoroutine(summary, recordingDataJson));
    }

    private IEnumerator UploadSessionDataCoroutine(SessionSummary summary, string recordingDataJson)
    {
        string url = $"{FirestoreBaseUrl}/sessions?key={apiKey}";

        string errorValues = "";
        if (summary.error_flags_detected != null && summary.error_flags_detected.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < summary.error_flags_detected.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{{\"stringValue\": \"{EscapeJson(summary.error_flags_detected[i])}\"}}");
            }
            errorValues = sb.ToString();
        }

        // Use ISO string timestamp for cross-platform compatibility with the dashboard
        string isoTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string firestoreJson = "{"
            + "\"fields\": {"
            + $"\"patient_id\": {{\"stringValue\": \"{EscapeJson(summary.patient_id)}\"}},"
            + $"\"patient_name\": {{\"stringValue\": \"{EscapeJson(summary.patient_name)}\"}},"
            + $"\"exercise\": {{\"stringValue\": \"{EscapeJson(summary.exercise)}\"}},"
            + $"\"timestamp\": {{\"stringValue\": \"{isoTimestamp}\"}},"
            + $"\"accuracy\": {{\"doubleValue\": {summary.accuracy}}},"
            + $"\"duration_sec\": {{\"integerValue\": \"{(int)Math.Round(summary.duration_sec)}\"}},"
            + $"\"total_reps\": {{\"integerValue\": \"{summary.total_reps}\"}},"
            + $"\"total_frames\": {{\"integerValue\": \"{summary.total_frames}\"}},"
            + $"\"error_flags\": {{\"arrayValue\": {{\"values\": [{errorValues}]}}}},"
            + $"\"recordingData\": {{\"stringValue\": \"{EscapeJson(recordingDataJson)}\"}}"
            + "}"
            + "}";

        using (UnityWebRequest request = CreatePostRequest(url, firestoreJson))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"[Firebase] Session uploaded for patient {summary.patient_id}.");
            else
                Debug.LogWarning($"[Firebase] Upload failed: {request.downloadHandler.text}");
        }
    }

    private string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private UnityWebRequest CreatePostRequest(string url, string jsonPayload)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}

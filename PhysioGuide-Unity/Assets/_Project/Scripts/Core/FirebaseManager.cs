using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
// Session data → Supabase | Patient/Therapist data → Firestore

/// <summary>
/// Handles Supabase operations for session uploads and storage.
/// Replaces Firebase Firestore with Supabase REST API.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    [Header("Supabase Configuration")]
    [Tooltip("Supabase project URL (e.g. https://xxxxx.supabase.co)")]
    public string supabaseUrl = "https://yuqrjilbdvxcenivppci.supabase.co";
    [Tooltip("Supabase anon (public) key")]
    public string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inl1cXJqaWxiZHZ4Y2VuaXZwcGNpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc0NDA4MzMsImV4cCI6MjA5MzAxNjgzM30.r94G_PBHG5xab1-jBdiSgtOxdtnD_7lD4r1To5Kp9L0";
    [Tooltip("Default Therapist UID")]
    public string defaultTherapistId = "YOUR_THERAPIST_UID";

    [Header("Supabase Storage")]
    [Tooltip("Supabase Storage bucket name")]
    public string supabaseBucket = "session-recordings";

    // Patient Data (Cached)
    public string PatientId { get; private set; } = "";
    public string PatientName { get; private set; } = "";
    public string PatientTherapistId { get; private set; } = "";
    public bool IsAuthenticated => !string.IsNullOrEmpty(PatientId);

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
        PatientTherapistId = PlayerPrefs.GetString("PatientTherapistId", "");

        // Use default therapist ID if not restored from prefs
        if (string.IsNullOrEmpty(PatientTherapistId) && !string.IsNullOrEmpty(defaultTherapistId) && defaultTherapistId != "YOUR_THERAPIST_UID")
        {
            PatientTherapistId = defaultTherapistId;
        }
    }

    public void SetTherapistId(string therapistId)
    {
        if (!string.IsNullOrEmpty(therapistId))
        {
            PatientTherapistId = therapistId;
            PlayerPrefs.SetString("PatientTherapistId", therapistId);
            PlayerPrefs.Save();
            Debug.Log($"[Supabase] Set therapist ID: {therapistId}");
        }
    }

    // ─── PATIENT VERIFICATION (SUPABASE) ────────────────────────

    /// <summary>
    /// Verifies a patient ID/key entered in the Unity login screen.
    /// The therapist dashboard creates patients with a unique_key (e.g. "ABC123").
    /// The patient document ID in Firestore is "pat_{key}".
    /// This method accepts either the raw key or the full "pat_" prefixed ID,
    /// queries Supabase to confirm the patient exists (has sessions or metadata),
    /// and caches the patient info on success.
    /// Callback: (bool success, string patientId, string patientName/errorMsg)
    /// </summary>
    public void VerifyPatientId(string inputKey, Action<bool, string, string> callback)
    {
        StartCoroutine(VerifyPatientIdCoroutine(inputKey, callback));
    }

    private IEnumerator VerifyPatientIdCoroutine(string inputKey, Action<bool, string, string> callback)
    {
        if (string.IsNullOrEmpty(inputKey))
        {
            callback?.Invoke(false, "", "Please enter a valid Patient ID.");
            yield break;
        }

        // Normalize: if user enters raw key like "ABC123", convert to "pat_ABC123"
        string patientId = inputKey.Trim();
        if (!patientId.StartsWith("pat_"))
        {
            patientId = "pat_" + patientId;
        }

        Debug.Log($"[Supabase] Verifying patient ID: {patientId}");

        // Query Supabase sessions table to check if this patient has any records
        // This also serves as validation that the ID is known to the system
        string url = $"{supabaseUrl}/rest/v1/sessions?patient_id=eq.{patientId}&select=patient_id,patient_name&limit=1";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseBody = request.downloadHandler.text;
                Debug.Log($"[Supabase] Verify response: {responseBody}");

                // Try to extract patient_name from existing sessions
                string patientName = ExtractJsonString(responseBody, "patient_name");

                if (!string.IsNullOrEmpty(patientName) && patientName.Length > 0)
                {
                    // Patient found in sessions — verified with known name
                    Debug.Log($"[Supabase] Patient verified from sessions: {patientId} -> {patientName}");
                    CachePatientLogin(patientId, patientName);
                    callback?.Invoke(true, patientId, patientName);
                }
                else
                {
                    // No sessions yet — accept the key anyway (first-time login)
                    // The key was generated by the therapist dashboard, so trust it
                    string fallbackName = patientId.Replace("pat_", "Patient ");
                    Debug.Log($"[Supabase] No sessions found for {patientId} — accepting as first-time login.");
                    CachePatientLogin(patientId, fallbackName);
                    callback?.Invoke(true, patientId, fallbackName);
                }
            }
            else
            {
                Debug.LogError($"[Supabase] Patient verification failed: {request.error}");
                // Network error — allow offline login if patient was previously cached
                string cachedId = PlayerPrefs.GetString("PatientID", "");
                string cachedName = PlayerPrefs.GetString("PatientName", "");
                if (!string.IsNullOrEmpty(cachedId) && cachedId == patientId)
                {
                    Debug.Log($"[Supabase] Using cached patient data for offline login: {cachedId}");
                    CachePatientLogin(cachedId, cachedName);
                    callback?.Invoke(true, cachedId, cachedName);
                }
                else
                {
                    callback?.Invoke(false, "", $"Could not verify patient ID. Check your connection.");
                }
            }
        }
    }

    private void CachePatientLogin(string patientId, string patientName)
    {
        PatientId = patientId;
        PatientName = patientName;
        PlayerPrefs.SetString("PatientID", patientId);
        PlayerPrefs.SetString("PatientName", patientName);
        PlayerPrefs.Save();
        Debug.Log($"[Supabase] Cached login: ID={patientId}, Name={patientName}");
    }

    // ─── SUPABASE STORAGE UPLOAD ──────────────────────────────

    /// <summary>
    /// Uploads a local recording JSON file to Supabase Storage with gzip compression.
    /// On success, calls back with (true, publicDownloadUrl). On failure, (false, errorMsg).
    /// </summary>
    public void UploadRecordingToStorage(string localFilePath, string storagePath, Action<bool, string> callback)
    {
        StartCoroutine(UploadRecordingToStorageCoroutine(localFilePath, storagePath, callback));
    }

    private IEnumerator UploadRecordingToStorageCoroutine(string localFilePath, string storagePath, Action<bool, string> callback)
    {
        if (!File.Exists(localFilePath))
        {
            Debug.LogError($"[Supabase Storage] File not found: {localFilePath}");
            callback?.Invoke(false, "File not found");
            yield break;
        }

        // Read and gzip-compress the file
        byte[] rawBytes = File.ReadAllBytes(localFilePath);
        byte[] compressedBytes;

        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gzipStream.Write(rawBytes, 0, rawBytes.Length);
            }
            compressedBytes = outputStream.ToArray();
        }

        float compressionRatio = rawBytes.Length > 0 ? (float)compressedBytes.Length / rawBytes.Length * 100f : 0f;
        Debug.Log($"[Supabase Storage] Gzip: {rawBytes.Length} -> {compressedBytes.Length} bytes ({compressionRatio:F1}%)");

        // Upload to Supabase Storage REST API
        string url = $"{supabaseUrl}/storage/v1/object/{supabaseBucket}/{storagePath}";

        Debug.Log($"[Supabase Storage] Uploading to: {supabaseBucket}/{storagePath}");
        Debug.Log($"[Supabase Storage] URL: {url.Substring(0, Math.Min(url.Length, 120))}...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(compressedBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/gzip");
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
            request.SetRequestHeader("x-upsert", "true");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string downloadUrl = $"{supabaseUrl}/storage/v1/object/public/{supabaseBucket}/{storagePath}";
                Debug.Log($"[Supabase Storage] Upload success! URL: {downloadUrl.Substring(0, Math.Min(downloadUrl.Length, 120))}...");
                callback?.Invoke(true, downloadUrl);
            }
            else
            {
                string errorBody = request.downloadHandler?.text ?? "(no body)";
                Debug.LogError($"[Supabase Storage] Upload failed: HTTP {request.responseCode} - {request.error}");
                Debug.LogError($"[Supabase Storage] Response: {errorBody.Substring(0, Math.Min(errorBody.Length, 500))}");
                callback?.Invoke(false, request.error);
            }
        }
    }

    // ─── SESSION UPLOAD (SUPABASE) ───────────────────────────────────

    public void UploadSessionData(SessionSummary summary, string recordingDataJson, string localRecordingPath = null)
    {
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║         [Supabase] SESSION UPLOAD STARTED                   ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log($"[Supabase] UploadSessionData called.");
        Debug.Log($"[Supabase]   summary.patient_id    = '{summary?.patient_id}'");
        Debug.Log($"[Supabase]   summary.patient_name  = '{summary?.patient_name}'");
        Debug.Log($"[Supabase]   summary.exercise      = '{summary?.exercise}'");
        Debug.Log($"[Supabase]   summary.accuracy      = {summary?.accuracy}");
        Debug.Log($"[Supabase]   summary.duration_sec  = {summary?.duration_sec}");
        Debug.Log($"[Supabase]   summary.total_reps    = {summary?.total_reps}");
        Debug.Log($"[Supabase]   summary.total_frames  = {summary?.total_frames}");
        Debug.Log($"[Supabase]   Cached PatientTherapistId = '{PatientTherapistId}'");

        if (summary == null)
        {
            Debug.LogError("[Supabase] ABORT: summary is null!");
            return;
        }

        StartCoroutine(UploadSessionDataCoroutine(summary, recordingDataJson, localRecordingPath));
    }

    private IEnumerator UploadSessionDataCoroutine(SessionSummary summary, string recordingDataJson, string localRecordingPath)
    {
        // ── Resolve therapist ID ──
        string therapistId = PatientTherapistId;
        if (string.IsNullOrEmpty(therapistId))
        {
            therapistId = defaultTherapistId;
        }

        if (string.IsNullOrEmpty(therapistId) || therapistId == "YOUR_THERAPIST_UID")
        {
            Debug.LogError($"[Supabase] ABORT: therapistId is invalid! Value='{therapistId}'.");
            yield break;
        }

        // ── Resolve patient ID ──
        string patientId = !string.IsNullOrEmpty(summary.patient_id) ? summary.patient_id : PatientId;

        if (string.IsNullOrEmpty(patientId))
        {
            Debug.LogError("[Supabase] ABORT: patientId is empty! No patient is logged in.");
            yield break;
        }

        // ── Step 1: Upload recording to Supabase Storage (gzip compressed) ──
        string recordingUrl = "";

        if (!string.IsNullOrEmpty(localRecordingPath) && File.Exists(localRecordingPath))
        {
            string storageFileName = Path.GetFileName(localRecordingPath);
            string storagePath = $"sessions/{therapistId}/{patientId}/{storageFileName}.gz";

            Debug.Log($"[Supabase] Uploading recording to Storage: {storagePath}");

            bool uploadDone = false;
            bool uploadSuccess = false;
            string uploadResult = "";

            UploadRecordingToStorage(localRecordingPath, storagePath, (success, result) =>
            {
                uploadSuccess = success;
                uploadResult = result;
                uploadDone = true;
            });

            while (!uploadDone)
                yield return null;

            if (uploadSuccess)
            {
                recordingUrl = uploadResult;
                summary.recording_url = recordingUrl;
            }
        }

        // ── Step 2: Build Supabase JSON payload ──
        string errorFlagsJson = "[]";
        if (summary.error_flags_detected != null && summary.error_flags_detected.Count > 0)
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < summary.error_flags_detected.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"{EscapeJson(summary.error_flags_detected[i])}\"");
            }
            sb.Append("]");
            errorFlagsJson = sb.ToString();
        }

        string isoTimestamp = DateTime.UtcNow.ToString("o");

        string supabaseJson = "{"
            + $"\"patient_id\": \"{EscapeJson(patientId)}\","
            + $"\"therapist_id\": \"{EscapeJson(therapistId)}\","
            + $"\"patient_name\": \"{EscapeJson(summary.patient_name)}\","
            + $"\"exercise\": \"{EscapeJson(summary.exercise)}\","
            + $"\"timestamp\": \"{isoTimestamp}\","
            + $"\"accuracy\": {summary.accuracy.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},"
            + $"\"duration_sec\": {(int)Math.Round(summary.duration_sec)},"
            + $"\"total_reps\": {summary.total_reps},"
            + $"\"total_frames\": {summary.total_frames},"
            + $"\"error_flags\": {errorFlagsJson},"
            + $"\"recording_url\": {(string.IsNullOrEmpty(recordingUrl) ? "null" : $"\"{EscapeJson(recordingUrl)}\"")},"
            + $"\"recording_filename\": \"{(string.IsNullOrEmpty(summary.recording_filename) ? "" : EscapeJson(summary.recording_filename))}\""
            + "}";

        // ── Step 3: POST to Supabase REST API ──
        string url = $"{supabaseUrl}/rest/v1/sessions";

        Debug.Log("┌──────────────────────────────────────────────────────────────┐");
        Debug.Log($"│ [Supabase] POSTING TO: {url}");
        Debug.Log("└──────────────────────────────────────────────────────────────┘");

        int previewLen = Math.Min(supabaseJson.Length, 600);
        Debug.Log($"[Supabase] JSON payload preview ({supabaseJson.Length} bytes):\n{supabaseJson.Substring(0, previewLen)}...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(supabaseJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
            request.SetRequestHeader("Prefer", "return=representation");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("╔══════════════════════════════════════════════════════════════╗");
                Debug.Log("║     [Supabase] ✅ SESSION UPLOAD SUCCESS                     ║");
                Debug.Log("╚══════════════════════════════════════════════════════════════╝");
            }
            else
            {
                Debug.LogError("╔══════════════════════════════════════════════════════════════╗");
                Debug.LogError("║     [Supabase] ❌ SESSION UPLOAD FAILED                      ║");
                Debug.LogError("╚══════════════════════════════════════════════════════════════╝");
                Debug.LogError($"[Supabase]   HTTP Status: {request.responseCode}");
                Debug.LogError($"[Supabase]   Error: {request.error}");
                string responseBody = request.downloadHandler?.text ?? "(no response body)";
                Debug.LogError($"[Supabase]   Response: {responseBody}");
            }
        }
    }

    // ─── FETCH PATIENT SESSIONS (SUPABASE) ────────────────────────────

    public void FetchPatientSessions(string patientId, Action<List<SessionSummary>> callback)
    {
        StartCoroutine(FetchPatientSessionsCoroutine(patientId, callback));
    }

    private IEnumerator FetchPatientSessionsCoroutine(string patientId, Action<List<SessionSummary>> callback)
    {
        string url = $"{supabaseUrl}/rest/v1/sessions?patient_id=eq.{patientId}&order=timestamp.desc";

        Debug.Log($"[Supabase] Fetching sessions for patient: {patientId}");

        List<SessionSummary> results = new List<SessionSummary>();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Supabase] Session fetch failed: {request.error} — {request.downloadHandler.text}");
            }
            else
            {
                string responseBody = request.downloadHandler.text;
                Debug.Log($"[Supabase] Sessions response length: {responseBody.Length}");
                results = ParseSupabaseSessions(responseBody);
                Debug.Log($"[Supabase] Parsed {results.Count} sessions for {patientId}");
            }
        }

        callback?.Invoke(results);
    }

    private List<SessionSummary> ParseSupabaseSessions(string json)
    {
        List<SessionSummary> sessions = new List<SessionSummary>();

        if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("["))
            return sessions;

        // Find each object in the array
        int searchFrom = 0;
        while (true)
        {
            int objStart = json.IndexOf("{", searchFrom);
            if (objStart == -1) break;

            int objEnd = json.IndexOf("}", objStart);
            if (objEnd == -1) break;

            string objJson = json.Substring(objStart, objEnd - objStart + 1);

            SessionSummary s = new SessionSummary();
            s.patient_id = ExtractJsonString(objJson, "patient_id");
            s.patient_name = ExtractJsonString(objJson, "patient_name");
            s.exercise = ExtractJsonString(objJson, "exercise");
            s.timestamp = ExtractJsonString(objJson, "timestamp");
            s.recording_filename = ExtractJsonString(objJson, "recording_filename");
            s.recording_url = ExtractJsonString(objJson, "recording_url");

            string accStr = ExtractJsonNumber(objJson, "accuracy");
            if (float.TryParse(accStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float acc))
                s.accuracy = acc;

            string durStr = ExtractJsonNumber(objJson, "duration_sec");
            if (float.TryParse(durStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float dur))
                s.duration_sec = dur;

            string repsStr = ExtractJsonNumber(objJson, "total_reps");
            if (int.TryParse(repsStr, out int reps))
                s.total_reps = reps;

            string framesStr = ExtractJsonNumber(objJson, "total_frames");
            if (int.TryParse(framesStr, out int frames))
                s.total_frames = frames;

            if (!string.IsNullOrEmpty(s.exercise))
            {
                sessions.Add(s);
            }

            searchFrom = objEnd + 1;
        }

        return sessions;
    }

    private string ExtractJsonString(string json, string fieldName)
    {
        string pattern = $"\"{fieldName}\":";
        int idx = json.IndexOf(pattern);
        if (idx == -1) return "";

        int startQuote = json.IndexOf("\"", idx + pattern.Length);
        if (startQuote == -1) return "";

        int endQuote = json.IndexOf("\"", startQuote + 1);
        if (endQuote == -1) return "";

        return json.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    private string ExtractJsonNumber(string json, string fieldName)
    {
        string pattern = $"\"{fieldName}\":";
        int idx = json.IndexOf(pattern);
        if (idx == -1)
        {
            pattern = $"{fieldName}:";
            idx = json.IndexOf(pattern);
            if (idx == -1) return "0";
        }

        string sub = json.Substring(idx + pattern.Length).TrimStart();
        StringBuilder num = new StringBuilder();
        foreach (char c in sub)
        {
            if (char.IsDigit(c) || c == '.' || c == '-')
                num.Append(c);
            else
                break;
        }
        return num.Length > 0 ? num.ToString() : "0";
    }

    private string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

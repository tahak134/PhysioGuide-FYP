using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject mainMenuPanel;
    public GameObject exerciseSelectionPanel;
    public GameObject historyPanel;
    public GameObject settingsPanel;

    [Header("Login UI Elements")]
    public TMP_InputField usernameInput;
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI loginErrorText;
    public GameObject loginButtonObj;       // To disable during auth
    public TextMeshProUGUI loginButtonLabel; // To show "Signing in..." text

    [Header("History UI Elements")]
    public TextMeshProUGUI historyContentText;
    [Header("Settings UI Elements")]
    public TMP_InputField scaptionDurationInput;
    public TMP_InputField shoulderFlexionDurationInput;
    public TMP_InputField ipAddressInput;

    private string currentPatientId = "";
    private string currentPatientName = "";
    private bool _isAuthenticating = false;

    void Start()
    {
        currentPatientId = PlayerPrefs.GetString("PatientID", "");
        currentPatientName = PlayerPrefs.GetString("PatientName", "");

        if (!string.IsNullOrEmpty(currentPatientId))
        {
            UpdateWelcomeText();
            ShowPanel(mainMenuPanel);
        }
        else
        {
            ShowPanel(loginPanel);
        }

        if (scaptionDurationInput != null)
            scaptionDurationInput.text = PlayerPrefs.GetFloat("ExerciseDuration_scaption", 60f).ToString();
        if (shoulderFlexionDurationInput != null)
            shoulderFlexionDurationInput.text = PlayerPrefs.GetFloat("ExerciseDuration_shoulder_flexion", 60f).ToString();
    }

    // --- PANEL NAVIGATION HELPER ---
    private void ShowPanel(GameObject panelToShow)
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (exerciseSelectionPanel) exerciseSelectionPanel.SetActive(false);
        if (historyPanel) historyPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        if (panelToShow) panelToShow.SetActive(true);
    }

    // --- LOGIN ---

    public void OnLoginClicked()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(username))
        {
            ShowLoginError("Patient ID is required!");
            return;
        }

        ShowLoginError("");
        SetLoginLoading(true);

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.VerifyPatientId(username, OnPatientVerified);
        }
        else
        {
            SetLoginLoading(false);
            ShowLoginError("Firebase authentication not configured properly.");
        }
    }

    private void OnPatientVerified(bool success, string patientId, string patientName)
    {
        SetLoginLoading(false);

        if (success)
        {
            Debug.Log($"[MainMenu] Verified Patient ID: {patientId}");
            currentPatientId = patientId;
            currentPatientName = patientName;
            
            PlayerPrefs.SetString("PatientID", currentPatientId);
            PlayerPrefs.SetString("PatientName", currentPatientName);
            PlayerPrefs.Save();
            
            UpdateWelcomeText();
            ShowPanel(mainMenuPanel);
        }
        else
        {
            ShowLoginError(patientName); // 'patientName' holds error message in failure case
        }
    }



    private void SetLoginLoading(bool loading)
    {
        _isAuthenticating = loading;

        if (loginButtonObj != null)
        {
            var btn = loginButtonObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = !loading;
        }

        if (loginButtonLabel != null)
            loginButtonLabel.text = loading ? "Signing in..." : "Login";
    }

    private void ShowLoginError(string message)
    {
        if (loginErrorText != null)
            loginErrorText.text = message;
    }

    // --- LOGOUT ---

    public void OnLogoutClicked()
    {
        PlayerPrefs.DeleteKey("PatientID");
        PlayerPrefs.DeleteKey("PatientName");
        PlayerPrefs.Save();

        currentPatientId = "";
        currentPatientName = "";

        if (usernameInput != null) usernameInput.text = "";

        ShowPanel(loginPanel);
    }

    // --- NAVIGATION ---

    public void OnPlayClicked()
    {
        ShowPanel(exerciseSelectionPanel);
    }

    public void OnHistoryClicked()
    {
        ShowPanel(historyPanel);
        LoadPatientHistory();
    }

    public void OnSettingsClicked()
    {
        ShowPanel(settingsPanel);
    }

    public void OnBackToMainMenuClicked()
    {
        ShowPanel(mainMenuPanel);
    }

    public void OnSaveSettingsClicked()
    {
        if (scaptionDurationInput != null && float.TryParse(scaptionDurationInput.text, out float scaDur))
        {
            PlayerPrefs.SetFloat("ExerciseDuration_scaption", scaDur);
            Debug.Log($"[MainMenuManager] Saved ExerciseDuration_scaption: {scaDur}");
        }
        if (shoulderFlexionDurationInput != null && float.TryParse(shoulderFlexionDurationInput.text, out float sfDur))
        {
            PlayerPrefs.SetFloat("ExerciseDuration_shoulder_flexion", sfDur);
            Debug.Log($"[MainMenuManager] Saved ExerciseDuration_shoulder_flexion: {sfDur}");
        }
        if (ipAddressInput != null && !string.IsNullOrEmpty(ipAddressInput.text))
        {
            string newIP = ipAddressInput.text.Trim();
            PlayerPrefs.SetString("TargetIP", newIP);
            
            if (ZMQClient.Instance != null)
            {
                ZMQClient.Instance.ConnectToIP(newIP);
            }
            Debug.Log($"[MainMenuManager] Saved Target IP: {newIP}");
        }
        PlayerPrefs.Save();
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting Application...");
        Application.Quit();
    }

    // --- EXERCISE SELECTION ---

    public void OnExerciseSelected(string exerciseName)
    {
        PlayerPrefs.SetString("SelectedExercise", exerciseName);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GymScene");
    }

    // --- HELPER METHODS ---

    private void UpdateWelcomeText()
    {
        if (welcomeText != null)
        {
            welcomeText.text = $"Welcome, {currentPatientName}!";
        }
    }

    private void LoadPatientHistory()
    {
        if (historyContentText == null) return;

        if (string.IsNullOrEmpty(currentPatientId))
        {
            historyContentText.text = "No patient logged in.";
            return;
        }

        // Show loading state
        historyContentText.text = "Loading session history...";

        // UI FIX: Dynamically ensure ContentSizeFitter is attached and set to PreferredSize
        UnityEngine.UI.ContentSizeFitter csf = historyContentText.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (csf == null)
        {
            csf = historyContentText.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        }
        csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        
        // Also ensure TextOverflow is Overflow not Truncate
        historyContentText.overflowMode = TMPro.TextOverflowModes.Overflow;

        // Try fetching from Firebase first
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.FetchPatientSessions(currentPatientId, OnSessionsFetched);
        }
        else
        {
            // Fallback to local files if Firebase is not available
            LoadLocalHistory();
        }
    }

    private void OnSessionsFetched(System.Collections.Generic.List<SessionSummary> sessions)
    {
        if (historyContentText == null) return;

        if (sessions == null || sessions.Count == 0)
        {
            // Fallback: try local files
            LoadLocalHistory();
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>{sessions.Count} Sessions Recorded</b>\n");

        int count = Mathf.Min(sessions.Count, 15);
        for (int i = 0; i < count; i++)
        {
            try
            {
                SessionSummary summary = sessions[i];
                string exercise = summary.exercise.Replace("_", " ");
                string date = "";
                try
                {
                    System.DateTime dt = System.DateTime.Parse(summary.timestamp);
                    date = dt.ToString("dd MMM yyyy, HH:mm");
                }
                catch { date = summary.timestamp; }

                string accuracyColor = summary.accuracy >= 80f ? "green" : (summary.accuracy >= 50f ? "yellow" : "red");

                sb.AppendLine($"<b>{i + 1}. {exercise}</b>  -  {date}");
                sb.AppendLine($"   Duration: {summary.duration_sec:F0}s  |  Reps: {summary.total_reps}  |  Accuracy: <color={accuracyColor}>{summary.accuracy:F1}%</color>");
                if (summary.error_flags_detected != null && summary.error_flags_detected.Count > 0)
                {
                    string errors = string.Join(", ", summary.error_flags_detected).Replace("_", " ");
                    sb.AppendLine($"   <color=red>Issues: {errors}</color>");
                }
                sb.AppendLine();
            }
            catch
            {
                sb.AppendLine($"{i + 1}. (could not parse session)\n");
            }
        }

        if (sessions.Count > count)
        {
            sb.AppendLine($"... and {sessions.Count - count} more sessions.");
        }

        historyContentText.text = sb.ToString();
    }

    /// <summary>
    /// Fallback: load session history from local Summary_*.json files.
    /// </summary>
    private void LoadLocalHistory()
    {
        if (historyContentText == null) return;

        string folderPath = Path.Combine(Application.persistentDataPath, "Sessions", currentPatientId);

        if (!Directory.Exists(folderPath))
        {
            historyContentText.text = "No history found for this patient.";
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "Summary_*.json");
        if (files.Length == 0)
        {
            historyContentText.text = "No recorded sessions yet.";
            return;
        }

        System.Array.Sort(files);
        System.Array.Reverse(files);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>{files.Length} Sessions Recorded</b>\n");

        int count = Mathf.Min(files.Length, 15);
        for (int i = 0; i < count; i++)
        {
            try
            {
                string json = File.ReadAllText(files[i]);
                SessionSummary summary = JsonUtility.FromJson<SessionSummary>(json);

                string exercise = summary.exercise.Replace("_", " ");
                string date = "";
                try
                {
                    System.DateTime dt = System.DateTime.Parse(summary.timestamp);
                    date = dt.ToString("dd MMM yyyy, HH:mm");
                }
                catch { date = Path.GetFileNameWithoutExtension(files[i]); }

                string accuracyColor = summary.accuracy >= 80f ? "green" : (summary.accuracy >= 50f ? "yellow" : "red");

                sb.AppendLine($"<b>{i + 1}. {exercise}</b>  -  {date}");
                sb.AppendLine($"   Duration: {summary.duration_sec:F0}s  |  Reps: {summary.total_reps}  |  Accuracy: <color={accuracyColor}>{summary.accuracy:F1}%</color>");
                if (summary.error_flags_detected != null && summary.error_flags_detected.Count > 0)
                {
                    string errors = string.Join(", ", summary.error_flags_detected).Replace("_", " ");
                    sb.AppendLine($"   <color=red>Issues: {errors}</color>");
                }
                sb.AppendLine();
            }
            catch
            {
                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                sb.AppendLine($"{i + 1}. {fileName} (could not parse)\n");
            }
        }

        if (files.Length > count)
        {
            sb.AppendLine($"... and {files.Length - count} more sessions.");
        }

        historyContentText.text = sb.ToString();
    }
}

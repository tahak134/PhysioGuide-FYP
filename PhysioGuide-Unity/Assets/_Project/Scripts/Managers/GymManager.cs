using System.Collections;
using TMPro;
using UnityEngine;

public class GymManager : MonoBehaviour
{
    [Header("References")]
    public TrainerPlayback trainer;
    public TextMeshProUGUI countdownText;
    public CalibrationManager calibrationManager;

    [Header("Configuration")]
    public string overrideExercise = "";

    [Header("Session Settings")]
    [Tooltip("Duration of the training phase in seconds. 0 = unlimited (manual stop).")]
    public float sessionDurationLimit = 60f;

    [Header("Calibration Settings")]
    [Tooltip("Max seconds to wait for T-pose during calibration")]
    public float calibrationTimeout = 15f;
    [Tooltip("Seconds the user must hold T-pose to confirm calibration")]
    public float calibrationHoldTime = 2f;

    private bool _sessionActive = false;

    // Cached UI references (found at Start via Transform.Find, works on inactive children)
    private GameObject _hudPanel;
    private GameObject _summaryPanel;

    [Header("Instruction UI")]
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionTitleText;
    public TextMeshProUGUI instructionBodyText;

    void Start()
    {
        string exercise = PlayerPrefs.GetString("SelectedExercise", "scaption");
        if (!string.IsNullOrEmpty(overrideExercise)) exercise = overrideExercise;

        // Read per-exercise duration (e.g. "ExerciseDuration_scaption"), fallback to generic key
        string durationKey = "ExerciseDuration_" + exercise;
        sessionDurationLimit = PlayerPrefs.GetFloat(durationKey, PlayerPrefs.GetFloat("ExerciseDuration", 60f));

        // Cache UI panels from canvas (Transform.Find works on inactive children)
        GameObject canvas = GameObject.Find("GymUI_OverlayCanvas");
        if (canvas != null)
        {
            Transform hudT = canvas.transform.Find("HUDPanel");
            if (hudT != null) _hudPanel = hudT.gameObject;
            Transform sumT = canvas.transform.Find("SummaryPanel");
            if (sumT != null) _summaryPanel = sumT.gameObject;
        }

        Debug.Log($"[GymManager] Setting up for: {exercise} with duration limit: {sessionDurationLimit}s, HUD cached: {_hudPanel != null}, Summary cached: {_summaryPanel != null}");

        // Configure trainer directly � don't rely on UIManager for this
        if (trainer != null)
        {
            trainer.SetExercise(exercise);
            trainer.isPlaying = false;
        }

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
            if (instructionTitleText != null)
                instructionTitleText.text = exercise == "scaption" ? "Scaption Exercise" : "Chin Tuck Exercise";
            if (instructionBodyText != null)
                instructionBodyText.text = exercise == "scaption"
                    ? "Stand facing the trainer avatar.\n\nRaise both arms in a V-shape in the scapular plane.\nKeep your elbows straight and thumbs pointing up.\n\nFollow the trainer's movement."
                    : "Stand facing the trainer avatar.\n\nKeep your shoulders still and pull your chin straight back.\nImagine making a 'double chin'.\n\nFollow the trainer's movement.";
        }
        else
        {
            // No instruction panel assigned � go straight to session
            StartCoroutine(FullSessionRoutine());
        }
    }

    public void OnAcceptInstructionsClicked()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
        StartCoroutine(FullSessionRoutine());
    }

    IEnumerator FullSessionRoutine()
    {
        // --- PHASE 1: SETUP ---
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Setup);

        if (countdownText) countdownText.text = "Get Ready...";
        yield return new WaitForSeconds(2f);

        // --- PHASE 2: CALIBRATION ---
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Calibration);

        yield return StartCoroutine(CalibrationRoutine());

        // --- PHASE 3: COUNTDOWN -> TRAINING ---
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Training);
            
        if (UIManager.Instance != null) {
            if (UIManager.Instance.CalibrationPanel != null) UIManager.Instance.CalibrationPanel.SetActive(false);
            if (UIManager.Instance.HUDPanel != null) UIManager.Instance.HUDPanel.SetActive(true);
        }
        // Always use cached reference as well (covers isolated scene testing)
        if (_hudPanel != null) _hudPanel.SetActive(true);

        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("3", false, 0);
        if (countdownText) countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("2", false, 0);
        if (countdownText) countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("1", false, 0);
        if (countdownText) countdownText.text = "1";
        yield return new WaitForSeconds(1f);
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("EXERCISE!", false, 0);
        if (countdownText) countdownText.text = "EXERCISE!";

        _sessionActive = true;

        if (trainer != null)
            trainer.PlayRecording();

        yield return new WaitForSeconds(1f);
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("", false, 0);
        if (countdownText) countdownText.text = "";

        // --- PHASE 3b: WAIT FOR SESSION TO END ---
        float timer = 0f;
        while (_sessionActive)
        {
            if (Time.timeScale > 0f) // Only tick trainer and timer if not paused
            {
                if (sessionDurationLimit > 0 && timer >= sessionDurationLimit)
                    break;
                
                if (trainer != null && !trainer.isPlaying)
                    break; // Automatically end if trainer finishes
            }

            timer += Time.deltaTime;
            yield return null;
        }

        EndSession();
    }

    IEnumerator CalibrationRoutine()
    {
        // If no calibration manager or no ZMQ data, skip calibration
        if (calibrationManager == null || ZMQClient.Instance == null)
        {
            if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("Skipping Calibration...", false, 0);
            if (countdownText) countdownText.text = "Skipping Calibration...";
            yield return new WaitForSeconds(1.5f);
            yield break;
        }

        calibrationManager.ResetCalibration();
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("Stand in T-Pose\nArms Out, Palms Down", false, 0);
        if (countdownText) countdownText.text = "Stand in T-Pose\nArms Out, Palms Down";
        bool isCalibrated = false;

        float elapsed = 0f;
        float holdTimer = 0f;

        while (elapsed < calibrationTimeout)
        {
            elapsed += Time.deltaTime;

            string json = ZMQClient.Instance.LatestPoseJson;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    PoseData pose = JsonUtility.FromJson<PoseData>(json);
                    if (pose != null && calibrationManager.TryCalibrate(pose))
                    {
                        holdTimer += Time.deltaTime;
                        float remaining = calibrationHoldTime - holdTimer;
                        
                        string txt = $"Hold T-Pose... {remaining:F1}s";
                        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback(txt, false, 0);
                        if (countdownText) countdownText.text = txt;

                        if (holdTimer >= calibrationHoldTime)
                        {
                            isCalibrated = true;
                        }
                    }
                    else
                    {
                        holdTimer = 0f; // Reset if pose is lost
                    }
                }
                catch { /* Skip bad frames */ }
            }

            if (isCalibrated)
            {
                if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("Calibrated!", false, 0);
                if (countdownText) countdownText.text = "Calibrated!";
                yield return new WaitForSeconds(1f);
                yield break;
            }

            yield return null;
        }

        // Timeout  proceed with default calibration
        if (UIManager.Instance != null) UIManager.Instance.UpdateLiveFeedback("Using Default Calibration", false, 0);
        if (countdownText) countdownText.text = "Using Default Calibration";
        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// Call this to end the training session and transition to Summary.
    /// Can be called from a UI button or automatically after sessionDurationLimit.
    /// </summary>
    public void EndSession()
    {
        Time.timeScale = 1f; // Ensure time scale is restored

        if (!_sessionActive) return;
        _sessionActive = false;

        if (trainer != null)
            trainer.isPlaying = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Summary);

        // Deactivate HUD using cached reference
        if (_hudPanel != null) _hudPanel.SetActive(false);
        else if (UIManager.Instance != null && UIManager.Instance.HUDPanel != null)
            UIManager.Instance.HUDPanel.SetActive(false);

        // Activate SummaryPanel using cached reference
        if (_summaryPanel != null)
            _summaryPanel.SetActive(true);
        else if (UIManager.Instance != null && UIManager.Instance.SummaryPanel != null)
            UIManager.Instance.SummaryPanel.SetActive(true);
        else
            Debug.LogError("[GymManager] SummaryPanel could not be found!");

        // Populate summary screen
        if (UIManager.Instance != null)
        {
            if (SessionLogger.Instance != null && SessionLogger.Instance.LastSessionSummary != null)
            {
                UIManager.Instance.ShowSessionSummary(SessionLogger.Instance.LastSessionSummary);
            }
            else
            {
                SessionSummary dummy = new SessionSummary {
                    patient_id = PlayerPrefs.GetString("PatientID", "isolate_test"),
                    duration_sec = GameManager.Instance != null ? GameManager.Instance.SessionDuration : 0f,
                    accuracy = 100f,
                    error_flags_detected = new System.Collections.Generic.List<string>()
                };
                UIManager.Instance.ShowSessionSummary(dummy);
            }
        }

        if (countdownText) countdownText.text = "";
        Debug.Log("[GymManager] Session complete. Showing summary.");
    }

    /// <summary>
    /// Call from a "Return to Menu" button on the Summary screen.
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Ensure time scale is reset before loading
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    // --- HUD CONTROLS ---
    private bool _isPaused = false;
    
    public void TogglePause()
    {
        if (!_sessionActive) return;
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        
        if (UIManager.Instance != null) {
            UIManager.Instance.UpdateLiveFeedback(_isPaused ? "PAUSED" : "", false, 0);
        }
    }

    public void RequestStop()
    {
        if (!_sessionActive) return;
        Time.timeScale = 0f; // Pause immediately
        
        // Show confirmation panel using cached HUD reference
        Transform confirmPanel = null;
        if (_hudPanel != null)
            confirmPanel = _hudPanel.transform.Find("StopConfirmationPanel");
        else if (UIManager.Instance != null && UIManager.Instance.HUDPanel != null)
            confirmPanel = UIManager.Instance.HUDPanel.transform.Find("StopConfirmationPanel");

        if (confirmPanel != null) confirmPanel.gameObject.SetActive(true);
        else EndSession(); // Fallback if no panel exists
    }

    public void ConfirmStop()
    {
        EndSession();
    }

    public void CancelStop()
    {
        Time.timeScale = _isPaused ? 0f : 1f; // Restore to current pause state
        
        Transform confirmPanel = null;
        if (_hudPanel != null)
            confirmPanel = _hudPanel.transform.Find("StopConfirmationPanel");
        else if (UIManager.Instance != null && UIManager.Instance.HUDPanel != null)
            confirmPanel = UIManager.Instance.HUDPanel.transform.Find("StopConfirmationPanel");

        if (confirmPanel != null) confirmPanel.gameObject.SetActive(false);
    }
}
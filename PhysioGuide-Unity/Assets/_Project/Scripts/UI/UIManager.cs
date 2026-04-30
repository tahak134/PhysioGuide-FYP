using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Needed for Slider and Image

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main Panels")]
    public GameObject SetupPanel;
    public GameObject CalibrationPanel;
    public GameObject TrainingPanel;
    public GameObject HUDPanel;
    public GameObject SummaryPanel;

    [Header("References")]
    public TrainerPlayback trainer;

    [Header("Patient HUD (VR View)")]
    public TextMeshProUGUI FeedbackText;
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI RepCounterText; // Added from Jaweria's logic
    public Slider AccuracySlider;          // Added from Jaweria's logic

    [Header("Summary Screen")]
    public TextMeshProUGUI SummaryTitleText;
    public TextMeshProUGUI SummaryDetailsText;
    public TextMeshProUGUI SummaryErrorsText;

    [Header("Therapist HUD (Display 2)")]
    public TextMeshProUGUI TherapistLogText;   // Added
    public TextMeshProUGUI LiveMetricsText;    // Added

    void Awake() { Instance = this; }

    void Start()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChange;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChange;
        }
    }

    // Called by GymManager to configure the trainer for the selected exercise
    public void SelectExercise(string exerciseName)
    {
        Debug.Log($"UI: Selected Exercise {exerciseName}");

        // Tell the Trainer Avatar what to play
        if (trainer != null)
        {
            trainer.SetExercise(exerciseName);
        }
    }

    private void HandleStateChange(GameState state)
    {
        // Disable all panels first
        if (SetupPanel) SetupPanel.SetActive(false);
        if (CalibrationPanel) CalibrationPanel.SetActive(false);
        if (TrainingPanel) TrainingPanel.SetActive(false);
        if (HUDPanel) HUDPanel.SetActive(false);
        if (SummaryPanel) SummaryPanel.SetActive(false);

        // Enable specific panel
        switch (state)
        {
            case GameState.Setup:
                if (SetupPanel) SetupPanel.SetActive(true);
                break;
            case GameState.Calibration:
                if (CalibrationPanel) CalibrationPanel.SetActive(true);
                break;
            case GameState.Training:
                // IMPORTANT: In your logic, is "Training" the Menu or the Exercise?
                // Usually: 
                // TrainingPanel = The Menu where you pick Scaption/ChinTuck
                // HUDPanel = The overlay while exercising

                // If we just clicked "Start", we want the HUD.
                if (HUDPanel) HUDPanel.SetActive(true);
                // Reset accuracy slider at session start
                if (AccuracySlider != null) AccuracySlider.value = 1f;
                break;
            case GameState.Summary:
                if (SummaryPanel) SummaryPanel.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Populates the Summary screen with session results.
    /// Called by GymManager after the session ends.
    /// </summary>
    public void ShowSessionSummary(SessionSummary summary)
    {
        Debug.Log($"[UIManager] ShowSessionSummary called. summary={(summary == null ? "NULL" : "valid")}");
        
        if (summary == null)
        {
            Debug.LogError("[UIManager] SessionSummary is NULL! Check GymManager or SessionLogger.");
            if (SummaryDetailsText != null)
                SummaryDetailsText.text = "Error: No session data available.";
            return;
        }

        Debug.Log($"[UIManager] Populating summary - Patient: {summary.patient_name}, Exercise: {summary.exercise}, " +
                  $"Reps: {summary.total_reps}, Accuracy: {summary.accuracy:F1}%, Frames: {summary.total_frames}");

        if (SummaryTitleText != null)
        {
            SummaryTitleText.text = "Session Complete!";
        }

        if (SummaryDetailsText != null)
        {
            string exerciseDisplay = summary.exercise.Replace("_", " ");
            exerciseDisplay = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(exerciseDisplay);

            SummaryDetailsText.text =
                $"<b>Patient:</b> {summary.patient_name}\n" +
                $"<b>Exercise:</b> {exerciseDisplay}\n" +
                $"<b>Duration:</b> {summary.duration_sec:F1}s\n" +
                $"<b>Repetitions:</b> {summary.total_reps}\n" +
                $"<b>Accuracy:</b> {summary.accuracy:F1}%\n" +
                $"<b>Frames Recorded:</b> {summary.total_frames}";
        }
        else
        {
            Debug.LogError("[UIManager] SummaryDetailsText is NOT assigned in Inspector!");
        }

        if (SummaryErrorsText != null)
        {
            if (summary.error_flags_detected != null && summary.error_flags_detected.Count > 0)
            {
                string errors = string.Join(", ", summary.error_flags_detected);
                SummaryErrorsText.text = $"<b>Issues Detected:</b>\n{errors.Replace("_", " ")}";
                SummaryErrorsText.color = Color.red;
            }
            else
            {
                SummaryErrorsText.text = "<b>Perfect Form! No issues detected.</b>";
                SummaryErrorsText.color = Color.green;
            }
        }
    }

    // Merged Update Logic: Handles Text, Color, Reps, and Slider
    public void UpdateLiveFeedback(string message, bool isError, int reps)
    {
        if (FeedbackText != null)
        {
            FeedbackText.text = message;
            FeedbackText.color = isError ? Color.red : Color.green;
        }

        if (RepCounterText != null)
        {
            RepCounterText.text = $"Reps: {reps}";
        }

        // Visual Slider Logic (Jaweria's feature)
        if (AccuracySlider != null)
        {
            if (isError) AccuracySlider.value -= 0.005f; // Drop slightly on error
            else AccuracySlider.value += 0.002f;         // Gain slightly on success
            AccuracySlider.value = Mathf.Clamp01(AccuracySlider.value);
        }
    }

    // New: Updates the Therapist's screen (Display 2)
    public void UpdateTherapistMetrics(float angleL, float angleR)
    {
        if (LiveMetricsText != null)
        {
            LiveMetricsText.text = $"<b>LIVE METRICS</b>\nL-Shoulder: {angleL:F1}\u00B0\nR-Shoulder: {angleR:F1}\u00B0";
        }
    }

    public void LogToTherapist(string logMessage)
    {
        if (TherapistLogText != null)
        {
            TherapistLogText.text += $"\n[{System.DateTime.Now:HH:mm:ss}] {logMessage}";
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Training)
        {
            if (TimerText) TimerText.text = $"Time: {GameManager.Instance.SessionDuration:F0}s";
        }
    }
}
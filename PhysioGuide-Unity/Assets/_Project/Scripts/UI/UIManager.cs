using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;


    public GameObject TherapistPanel;
    public TextMeshProUGUI TherapistLogText;


    public GameObject PatientCalibrationPanel;
    public GameObject PatientFeedbackPanel;
    public TextMeshProUGUI FeedbackText;

    void Awake() { Instance = this; }

    public void OnStateChanged(GameState state)
    {
        // Hide all first
        PatientCalibrationPanel.SetActive(false);
        PatientFeedbackPanel.SetActive(false);

        // Show based on state
        if (state == GameState.Calibration)
            PatientCalibrationPanel.SetActive(true);
        else if (state == GameState.Training)
            PatientFeedbackPanel.SetActive(true);
    }

    public void UpdateFeedback(string message, bool isError)
    {
        FeedbackText.text = message;
        FeedbackText.color = isError ? Color.red : Color.green;
        TherapistLogText.text = $"Live: {message}";
    }
}
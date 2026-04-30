using System;
using UnityEngine;

// Define the distinct phases of the rehabilitation session
public enum GameState { Setup, Calibration, Training, Summary }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // --- Event Bus ---
    public event Action<GameState> OnStateChanged;
    public event Action OnSessionStart;
    public event Action OnSessionEnd;

    public GameState CurrentState { get; private set; }

    public float SessionDuration = 0f;
    public int RepetitionCount = 0;

    // Reference to the calibration logic
    public CalibrationManager calibrationManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        SetState(GameState.Setup);
    }

    void Update()
    {
        if (CurrentState == GameState.Training)
        {
            SessionDuration += Time.deltaTime;
        }

        // Debug Inputs for testing flow in Editor
        if (Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetState(GameState.Setup);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetState(GameState.Calibration);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetState(GameState.Training);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetState(GameState.Summary);
        }
    }

    /// <summary>
    /// The primary method to change the application state.
    /// Handles entry/exit logic and notifies all listeners.
    /// </summary>
    public void SetState(GameState newState)
    {
        // Exit Logic: Clean up the previous state
        if (CurrentState == GameState.Training && newState != GameState.Training)
        {
            OnSessionEnd?.Invoke();
            Debug.Log($"Session Ended. Duration: {SessionDuration:F2}s");
        }

        CurrentState = newState;

        // Enter Logic: Setup the new state
        switch (newState)
        {
            case GameState.Setup:
                SessionDuration = 0;
                RepetitionCount = 0;
                break;
            case GameState.Training:
                if (Time.timeScale <= 0f)
                {
                    Debug.LogWarning($"[GameManager] Time.timeScale was {Time.timeScale}, resetting to 1 for training session.");
                }
                Time.timeScale = 1f;
                OnSessionStart?.Invoke();
                break;
        }

        OnStateChanged?.Invoke(newState);
        Debug.Log($"State Changed to: {newState}");
    }

    public void IncrementRepetition()
    {
        RepetitionCount++;
    }
}
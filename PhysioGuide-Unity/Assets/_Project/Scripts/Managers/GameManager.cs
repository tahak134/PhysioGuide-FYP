using UnityEngine;

public enum GameState { Setup, Calibration, Training, Summary }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState CurrentState;

    public float SessionDuration = 0f;
    public int RepetitionCount = 0;

    void Awake() { Instance = this; }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        // Notify other scripts (UIManager, SessionLogger) that state changed
        UIManager.Instance.OnStateChanged(newState);

        if (newState == GameState.Training)
        {
            SessionLogger.Instance.StartLogging();
        }
        else if (newState == GameState.Summary)
        {
            SessionLogger.Instance.StopLogging();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class TrainerPlayback : MonoBehaviour
{
    public enum ExerciseMode { Scaption, ChinTuck }

    [Header("Inspector Controls")]
    public ExerciseMode selectedExercise = ExerciseMode.Scaption;

    [Header("Recording Files")]
    public TextAsset scaptionFile; 
    public TextAsset chinTuckFile; 

    [Header("References")]
    public AvatarController avatarController;

    [Header("Settings")]
    public float frameRate = 30f; // Target FPS for playback
    public bool isPlaying = false;
    public bool isLooping = true;

    private List<PoseData> _currentSequence = new List<PoseData>();
    private int _frameIndex = 0;
    private float _timer = 0f;

    private ExerciseMode _lastMode;

    void Start()
    {
        if (avatarController == null) Debug.LogError("[TrainerPlayback] CRITICAL: AvatarController slot is EMPTY!");
        SetExercise(selectedExercise);
        _lastMode = selectedExercise;
        // Don't auto-play — GymManager calls PlayRecording() after countdown
    }

    void Update()
    {
        if (selectedExercise != _lastMode)
        {
            UnityEngine.Debug.Log($"[TrainerPlayback] Switching to {selectedExercise}...");
            SetExercise(selectedExercise);
            _lastMode = selectedExercise;
        }

        if (!isPlaying || _currentSequence.Count == 0 || avatarController == null) return;

        // Timer logic to keep playback speed consistent (30 FPS)
        _timer += Time.deltaTime;
        if (_timer >= (1f / frameRate))
        {
            _timer = 0f; // Reset timer

            // 1. Get current frame data
            PoseData data = _currentSequence[_frameIndex];
            if (string.IsNullOrEmpty(data.exercise))
            {
                data.exercise = (selectedExercise == ExerciseMode.ChinTuck) ? "chin_tuck" : "scaption";
            }
            avatarController.UpdatePose(data);

            int nextFrame = _frameIndex + 1;

            // 5. Handle Looping
            if (nextFrame >= _currentSequence.Count)
            {
                if (isLooping)
                {
                    _frameIndex = 0; // Loop back to start
                }
                else
                {
                    _frameIndex = _currentSequence.Count - 1; // Stay at end
                    isPlaying = false; // Stop
                }
            }
            else
            {
                _frameIndex = nextFrame;
            }
        }
    }

    // --- CALLED BY GYM MANAGER ---
    public void PlayRecording()
    {
        _frameIndex = 0;
        _timer = 0f;
        isPlaying = true;
        UnityEngine.Debug.Log("[TrainerPlayback] Playing started!");
    }


    public void SetExercise(ExerciseMode mode)
    {
        isPlaying = false;
        _currentSequence.Clear();
        _frameIndex = 0;
        selectedExercise = mode;

        if (mode == ExerciseMode.ChinTuck)
        {
            if (chinTuckFile != null) LoadRecording(chinTuckFile);
            else Debug.LogWarning("No Chin Tuck File assigned!");
        }
        else
        {
            if (scaptionFile != null) LoadRecording(scaptionFile);
            else Debug.LogWarning("No Scaption File assigned!");
        }
        // Don't auto-play — GymManager calls PlayRecording() after countdown
    }

    public void SetExercise(string exerciseName)
    {
        if (string.IsNullOrEmpty(exerciseName)) return;

        Debug.Log($"[TrainerPlayback] External request for '{exerciseName}'");
        if (exerciseName.ToLower().Contains("chin")) SetExercise(ExerciseMode.ChinTuck);
        else SetExercise(ExerciseMode.Scaption);
    }

    private void LoadRecording(TextAsset file)
    {
        _currentSequence.Clear();
        if (file == null) return;

        // STRATEGY 1: Try splitting by new line (NDJSON format)
        string[] lines = file.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.Trim().StartsWith("{"))
            {
                try
                {
                    PoseData pose = JsonUtility.FromJson<PoseData>(line);
                    if (pose != null) _currentSequence.Add(pose);
                }
                catch { /* Ignore bad lines */ }
            }
        }

        if (_currentSequence.Count == 0)
        {
            try
            {
                // We wrap it because Unity JsonUtility cannot parse top-level arrays directly
                string wrappedJson = "{\"items\":" + file.text + "}";
                Wrapper<PoseData> wrapper = JsonUtility.FromJson<Wrapper<PoseData>>(wrappedJson);
                if (wrapper != null && wrapper.items != null)
                {
                    _currentSequence.AddRange(wrapper.items);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"JSON Parse Error: {e.Message}");
            }
        }

        Debug.Log($"[TrainerPlayback] Loaded {file.name}: {_currentSequence.Count} frames. Ready to play.");
        // Don't set isPlaying here — caller controls when playback begins
    }

    // Helper class for Strategy 2
    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}
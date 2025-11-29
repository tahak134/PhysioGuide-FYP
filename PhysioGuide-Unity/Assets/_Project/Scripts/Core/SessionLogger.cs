using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance;
    private List<PoseData> _sessionData = new List<PoseData>();
    private bool _isLogging = false;

    void Awake() { Instance = this; }

    public void StartLogging()
    {
        _sessionData.Clear();
        _isLogging = true;
    }

    public void StopLogging()
    {
        _isLogging = false;
        SaveToFile();
    }

    public void LogFrame(PoseData data)
    {
        if (_isLogging) _sessionData.Add(data);
    }

    private void SaveToFile()
    {
        // Using the JsonHelper class to handle list serialization
        string json = JsonHelper.ToJson(_sessionData.ToArray(), true);

        string path = Path.Combine(UnityEngine.Application.persistentDataPath, "Session_" + System.DateTime.Now.ToString("yyyyMMdd_HHmm") + ".json");

        File.WriteAllText(path, json);
        UnityEngine.Debug.Log("Session saved to: " + path);
    }
}
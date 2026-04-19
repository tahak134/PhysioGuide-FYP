using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class ZMQClient : MonoBehaviour
{
    public static ZMQClient Instance { get; private set; }

    [Header("Status")]
    public bool IsConnected = false;

    [Header("Settings")]
    public string targetIP = "10.200.248.104";
    public bool logConnectionStatus = true;

    // Thread-safe pose data access
    private readonly object _poseLock = new object();
    private string _latestPoseJson = "";

    public string LatestPoseJson
    {
        get { lock (_poseLock) { return _latestPoseJson; } }
        set { lock (_poseLock) { _latestPoseJson = value; } }
    }

    private Thread _clientThread;
    private volatile bool _isRunning = false;
    private volatile bool _restartNeeded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        _isRunning = false;
        if (_clientThread != null) _clientThread.Join(500);
    }

    // --- UI INTERFACE ---
    // This function allows your UIManager to change the IP and restart the thread
    public void ConnectToIP(string ipAddress)
    {
        targetIP = ipAddress;
        _restartNeeded = true;
    }

    void Start()
    {
        // Load saved IP if available, otherwise keep the Inspector default
        string savedIP = PlayerPrefs.GetString("TargetIP", "");
        if (!string.IsNullOrEmpty(savedIP))
        {
            targetIP = savedIP;
        }

        _isRunning = true;
        _clientThread = new Thread(NetMQWorker);
        _clientThread.IsBackground = true;
        _clientThread.Start();
    }

    private void NetMQWorker()
    {
        AsyncIO.ForceDotNet.Force();

        while (_isRunning)
        {
            _restartNeeded = false;

            try
            {
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 1000;
                    subSocket.Connect($"tcp://{targetIP}:5555");
                    subSocket.SubscribeToAnyTopic();

                    IsConnected = true;
                    if (logConnectionStatus) UnityEngine.Debug.Log($"[ZMQ] Connected to {targetIP}");

                    while (_isRunning && !_restartNeeded)
                    {
                        string message = null;
                        // Wait up to 100ms for a message, then loop
                        if (subSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out message))
                        {
                            LatestPoseJson = message;
                        }
                    }

                    subSocket.Disconnect($"tcp://{targetIP}:5555");
                    IsConnected = false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ZMQ ERROR] Thread Crashed: {e.Message}");
                IsConnected = false;
                Thread.Sleep(1000); // Wait 1s before retrying
            }

        }

        NetMQConfig.Cleanup();
    }
}
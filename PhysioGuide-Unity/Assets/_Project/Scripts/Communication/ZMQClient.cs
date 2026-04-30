using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
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
    private Thread _discoveryThread;
    private volatile bool _isRunning = false;
    private volatile bool _restartNeeded = false;

    private ConcurrentQueue<string> _commandQueue = new ConcurrentQueue<string>();

    public void SendCommand(string commandJson)
    {
        _commandQueue.Enqueue(commandJson);
    }

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

        _discoveryThread = new Thread(DiscoveryWorker);
        _discoveryThread.IsBackground = true;
        _discoveryThread.Start();
    }

    private void DiscoveryWorker()
    {
        try 
        {
            using (UdpClient udpClient = new UdpClient(5559))
            {
                udpClient.Client.ReceiveTimeout = 2000;
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 5559);
                while (_isRunning)
                {
                    try 
                    {
                        byte[] data = udpClient.Receive(ref remoteEP);
                        string message = System.Text.Encoding.UTF8.GetString(data);
                        if (message.Contains("PhysioGuide"))
                        {
                            string discoveredIP = remoteEP.Address.ToString();
                            if (targetIP != discoveredIP)
                            {
                                targetIP = discoveredIP;
                                PlayerPrefs.SetString("TargetIP", targetIP);
                                PlayerPrefs.Save();
                                _restartNeeded = true;
                                if (logConnectionStatus) UnityEngine.Debug.Log($"[AutoDiscovery] Found backend at {targetIP}");
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        // Timeout is expected, just loop
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[AutoDiscovery Error] {e.Message}");
        }
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
                using (var reqSocket = new RequestSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 1000;
                    subSocket.Connect($"tcp://{targetIP}:5555");
                    subSocket.SubscribeToAnyTopic();

                    reqSocket.Connect($"tcp://{targetIP}:5557");

                    // Add delay to prevent 'Slow Joiner' drop of initial messages
                    Thread.Sleep(500);
                    // Send a ping to confirm to python that Unity is attached
                    reqSocket.SendFrame("{\"command\": \"HELLO\"}");
                    string reply;
                    reqSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out reply);

                    IsConnected = true;
                    if (logConnectionStatus) UnityEngine.Debug.Log($"[ZMQ] Connected to {targetIP}. Waiting for frames...");
                    long frameReceiveCount = 0;
                    bool firstFrameLogged = false;

                    while (_isRunning && !_restartNeeded)
                    {
                        string message = null;
                        // Wait up to 50ms for a message, then loop and check queue
                        if (subSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(50), out message))
                        {
                            LatestPoseJson = message;
                            frameReceiveCount++;
                            
                            // Log first frame received
                            if (!firstFrameLogged)
                            {
                                UnityEngine.Debug.Log($"[ZMQ] FIRST FRAME RECEIVED! Count: {frameReceiveCount}");
                                firstFrameLogged = true;
                            }
                            
                            if (frameReceiveCount % 30 == 0 && logConnectionStatus)
                            {
                                UnityEngine.Debug.Log($"[ZMQ] Total frames received: {frameReceiveCount}");
                            }
                        }

                        while (_commandQueue.TryDequeue(out string cmd))
                        {
                            if (logConnectionStatus) UnityEngine.Debug.Log($"[ZMQ] Sending Command to {targetIP}:5557 -> {cmd}");
                            reqSocket.SendFrame(cmd);
                            
                            // Essential for REQ/REP pattern - must read reply
                            if (reqSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out string cmdReply)) {
                                if (logConnectionStatus) UnityEngine.Debug.Log($"[ZMQ] Backend Reply: {cmdReply}");
                            } else {
                                UnityEngine.Debug.LogWarning("[ZMQ] No reply from backend for command.");
                            }
                        }
                    }

                    subSocket.Disconnect($"tcp://{targetIP}:5555");
                    reqSocket.Disconnect($"tcp://{targetIP}:5557");
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
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// Manages the network connection to the Python OpenPose Server.
/// Runs the socket connection on a separate thread to prevent freezing the Unity UI.
/// </summary>
public class ZMQClient : MonoBehaviour
{
    // Thread-safe storage for the latest incoming data
    public static volatile string incomingPoseData = "";

    // Configuration
    // If Single Machine: use "tcp://localhost:5555"
    // If Dual Machine: use "tcp://192.168.X.X:5555" (Server's IP)

    public string serverAddress = "tcp://localhost:5555";

    private Thread _clientThread;
    private bool _isRunning = false;
    private RequestSocket _clientSocket;

    void Start()
    {
        _isRunning = true;
        _clientThread = new Thread(NetworkLoop);
        _clientThread.IsBackground = true; // Ensures thread dies if app closes
        _clientThread.Start();
    }

    private void NetworkLoop()
    {
        // NetMQ cleanup requirement
        AsyncIO.ForceDotNet.Force();

        try
        {
            using (_clientSocket = new RequestSocket())
            {
                _clientSocket.Connect(serverAddress);
                UnityEngine.Debug.Log($"[ZMQ] Connected to {serverAddress}");

                while (_isRunning)
                {
                    try
                    {
                        // 1. Send Request (Handshake)
                        _clientSocket.SendFrame("get_pose");

                        // 2. Receive Reply (with Timeout)
                        // We use a timeout so Unity doesn't hang if Python crashes
                        string message = null;
                        bool gotMessage = _clientSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out message);

                        if (gotMessage && !string.IsNullOrEmpty(message))
                        {
                            incomingPoseData = message;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log only occasionally to avoid flooding console
                        UnityEngine.Debug.LogError($"[ZMQ Error] {ex.Message}");
                    }

                    // Throttle the network loop. 
                    // 33ms roughly equals 30 requests per second.
                    Thread.Sleep(33);
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[ZMQ Critical Failure] {e.Message}");
        }
        finally
        {
            NetMQConfig.Cleanup();
        }
    }

    void OnDestroy()
    {
        _isRunning = false;
        if (_clientThread != null)
        {
            _clientThread.Join(500); // Wait 500ms for thread to shut down
        }
    }
}
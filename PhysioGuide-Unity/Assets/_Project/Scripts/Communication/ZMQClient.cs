using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using UnityEngine;

public class ZMQClient : MonoBehaviour
{
    // This is a static (shared) variable that any script can read 
    public static volatile string incomingPoseData;

    private Thread clientThread;
    private bool isRunning = true;

    void Start()
    {
        // Start the client in a separate thread 
        clientThread = new Thread(NetMQClient);
        clientThread.Start();
    }

    void NetMQClient()
    {
        using (var socket = new RequestSocket())
        {
            // Connect to Hussain's server (localhost if on same PC) 
            socket.Connect("tcp://localhost:5555");

            while (isRunning)
            {
                // Send a request to get the latest pose 
                socket.SendFrame("get_pose");

                // Receive the reply (the JSON data) 
                string data;
                if (socket.TryReceiveFrameString(out data))
                {
                    incomingPoseData = data; // Store it 
                }
                Thread.Sleep(30); // ~30 fps 
            }
        }
    }

    void OnDestroy()
    {
        // Clean up the thread when the game stops 
        isRunning = false;
        clientThread.Join();
    }
}

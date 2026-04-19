using UnityEngine;

public class DisplayActivator : MonoBehaviour
{
    void Start()
    {
        // Check if display 2 is available
        if (Display.displays.Length > 1)
        {
            // Activate display 2
            Display.displays[1].Activate();
        }

        // Note: Display 1 is always active by default.
        // You would use Display.displays[0].Activate() for Display 1, etc.
    }
}
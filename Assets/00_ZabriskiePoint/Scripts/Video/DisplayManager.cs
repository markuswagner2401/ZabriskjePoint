using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    [System.Serializable]
    public struct Display
    {
        public string name;
        public Camera camera;
        public int outputIndex;
    }

    public Display[] displays;

    void Start()
{
    
   

    // Get the number of displays connected to the graphics card
    int displayCount = UnityEngine.Display.displays.Length;

    // Loop through each connected display and activate it
    for (int i = 0; i < displayCount; i++)
    {
        UnityEngine.Display.displays[i].Activate();
    }

    // Loop through each configured display
    foreach (var display in displays)
    {
        // Check if the configured outputIndex is within the range of connected displays
        if (display.outputIndex >= 0 && display.outputIndex < displayCount)
        {
            // Set the camera's targetDisplay
            display.camera.targetDisplay = display.outputIndex;
        }
        else
        {
            Debug.LogWarning("Display outputIndex out of range for " + display.name);
        }
    }
}

    public void SetDisplay(Camera camera, int displayIndex)
    {
        // Check if the displayIndex is within the range of connected displays
        if (displayIndex >= 0 && displayIndex < UnityEngine.Display.displays.Length)
        {
            // Activate the display and set the camera's targetDisplay
            UnityEngine.Display.displays[displayIndex].Activate();
            camera.targetDisplay = displayIndex;
        }
        else
        {
            Debug.LogWarning("Display index out of range in SetDisplay()");
        }
    }
}

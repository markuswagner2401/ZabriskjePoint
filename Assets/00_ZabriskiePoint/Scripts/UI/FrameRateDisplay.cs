using UnityEngine;

public class FrameRateDisplay : MonoBehaviour
{
    // Variables to keep track of frame rate
    private float deltaTime = 0.0f;
    private float updateRate = 0.5f; // Update rate in seconds (adjust as needed)

    // GUI Style for displaying the frame rate
    private GUIStyle style;

    private void Start()
    {
        // Create a new GUIStyle for the FPS display
        style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
    }

    private void Update()
    {
        // Calculate the frame rate
        deltaTime += Time.deltaTime;
        deltaTime /= 2.0f;
    }

    private void OnGUI()
    {
        // Display the frame rate on the screen
        float fps = 1.0f / deltaTime;
        Rect rect = new Rect(10, 10, 100, 30);
        GUI.Label(rect, "FPS: " + Mathf.Round(fps).ToString(), style);
    }
}

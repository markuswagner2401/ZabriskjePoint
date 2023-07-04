using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    [Tooltip("Match this array with dropdown in display selector")]
    public Camera[] cameras;

    [System.Serializable]
    public struct Display
    {
        public string name;
        //public Camera camera;
        public DisplaySelector displaySelector;

    }

    [Tooltip("display index matches index of array element")]
    public Display[] displays;

    int displayCount;

    void Start()
    {
        UpdateDisplayCameraPatch();
    }



    /////

    public void UpdateDisplayCameraPatch()
    {
        UpdateDisplayCount();
        ActivateDisplays();
        SetCameras();
    }



    ////

    

    

    private void UpdateDisplayCount()
    {
        displayCount = UnityEngine.Display.displays.Length;
        MyConsole.Instance.Print("Verbundene Bildschirme: " + displayCount);
    }

    private void ActivateDisplays()
    {
        for (int i = 0; i < displayCount; i++)
        {
            UnityEngine.Display.displays[i].Activate();
        }
    }

    private void SetCameras()
    {
        for (int i = 0; i < displays.Length; i++)
        {
            SetCamera(i);
        }
    }

    private void SetCamera(int displayIndex)
    {
        //Check if the displayIndex is within the range of connected displays

        if (displayIndex >= 0 && displayIndex < UnityEngine.Display.displays.Length)
        {
            // Activate the display and set the camera's targetDisplay
            UnityEngine.Display.displays[displayIndex].Activate();
            int cameraIndex = displays[displayIndex].displaySelector.GetDropdownValue();

            if (cameraIndex == -1)
            {
                Debug.LogError("Please Assign a Dropdown Element in Display Selector for Display: " + displayIndex);
                return;
            }

            Camera targetCamera = cameras[cameraIndex];
            targetCamera.targetDisplay = displayIndex;

        }
        else
        {
            Debug.LogWarning("Display index out of range in SetDisplay()");
        }

    }


    ////







}

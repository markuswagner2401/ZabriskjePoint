using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using TMPro;

public class DeviceManager : MonoBehaviour
{
    [System.Serializable]
    public struct DeviceUse
    {
        public string useName;

        public TMP_Dropdown sensorSelector;

        public KinectManager kinectManager;
        public Kinect4AzureInterface kinect4AzureInterface;

        public TMP_Dropdown displaySelector;

        public Camera camera;


    }
    [SerializeField] DeviceUse[] deviceUses;

    int displayCount;

    List<Camera> activeCameras = new List<Camera>();

    private void Start()
    {
        UpdateDisplayCameraPatch();
    }

    // Displays

    public void UpdateDisplayCameraPatch()
    {
        UpdateDisplayCount();
        ActivateDisplays();
        PatchCameras();
    }

    private void UpdateDisplayCount()
    {
        displayCount = UnityEngine.Display.displays.Length;
        MyConsole.Instance.Print("Verbundene Displays: " + displayCount);
    }

    private void ActivateDisplays()
    {
        for (int i = 0; i < displayCount; i++)
        {
            UnityEngine.Display.displays[i].Activate();
        }
    }

    void PatchCameras()
    {
        activeCameras = new List<Camera>();
        
        for (int i = 0; i < deviceUses.Length; i++)
        {
            int displayIndex = deviceUses[i].displaySelector.value;
            if (displayIndex >= displayCount)
            {
                MyConsole.Instance.Print(deviceUses[i].useName + " kann nicht gezeigt werden. (Display nicht angeschlossen)" );
                if(activeCameras.Count > 0)
                {
                    deviceUses[i].camera.enabled = false;
                }
                 
            }
            else
            {
                deviceUses[i].camera.enabled = true;
                activeCameras.Add(deviceUses[i].camera);
                deviceUses[i].camera.targetDisplay = displayIndex;
            }
            
        }
    }

    // Kinect

    public void SetupKinect()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            if(deviceUses[i].kinectManager == null) continue;
            int kinectIndex = deviceUses[i].sensorSelector.value;
            deviceUses[i].kinect4AzureInterface.deviceIndex = deviceUses[i].sensorSelector.value;
            if(deviceUses[i].kinect4AzureInterface.IsSensorDataValid())
            {
                MyConsole.Instance.Print(deviceUses[i].useName + " Kinect Sensor is OK");
            }

            else
            {
                MyConsole.Instance.Print(deviceUses[i].useName + " Kinect Sensor is not OK");
            }
            
            
        }
    }
    

}



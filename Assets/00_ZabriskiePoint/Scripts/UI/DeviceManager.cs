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
        public DeviceSelector deviceSelector;
        public KinectManager kinectManager;
        public Kinect4AzureInterface kinect4AzureInterface;
        public Camera camera;

        public bool displayOK;

        public bool kinectOK;

    }
    [SerializeField] DeviceUse[] deviceUses;

    int displayCount;

    List<Camera> activeCameras = new List<Camera>();

    private void Start()
    {
        UpdateDisplayCameraPatch();
        SetupKinect();


    }



    // Displays

    public void UpdateDisplayCameraPatch()
    {
        UpdateDisplayCount();
        ActivateDisplays();
        PatchCameras();
        UpdateValidColors();
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
            int displayIndex = deviceUses[i].deviceSelector.GetDisplayDropdownValue();
            if (displayIndex >= displayCount)
            {
                //MyConsole.Instance.Print(deviceUses[i].useName + " kann nicht gezeigt werden. (Display nicht angeschlossen)" );
                deviceUses[i].displayOK = false;
                //deviceUses[i].deviceSelector.SetDisplayActiveColor(false);
                if (activeCameras.Count > 0)
                {
                    deviceUses[i].camera.enabled = false;
                }

            }
            else
            {
                deviceUses[i].camera.enabled = true;
                activeCameras.Add(deviceUses[i].camera);
                deviceUses[i].camera.targetDisplay = displayIndex;
                //
                deviceUses[i].displayOK = true;
            }

        }
    }

    // Kinect

    public void SetupKinect()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            if (deviceUses[i].kinectManager == null)
            {
                deviceUses[i].kinectOK = true;
                continue;
            }
            int kinectIndex = deviceUses[i].deviceSelector.GetSensorDropdownValue();

            List<KinectInterop.SensorDeviceInfo> alSensors = deviceUses[i].kinect4AzureInterface.GetAvailableSensors();
            if (kinectIndex >= alSensors.Count)
            {
                deviceUses[i].kinectOK = false;
            }
            else
            {
                deviceUses[i].kinectOK = true;
                deviceUses[i].kinect4AzureInterface.deviceIndex = kinectIndex;
            }
        }

        UpdateValidColors();
    }

    void UpdateValidColors()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            // display

            if (deviceUses[i].displayOK)
            {
                deviceUses[i].deviceSelector.SetDisplayActiveColor(true);
            }

            else
            {
                deviceUses[i].deviceSelector.SetDisplayActiveColor(false);
            }

            //kinect

            if (deviceUses[i].kinectOK)
            {
                deviceUses[i].deviceSelector.SetSensorActiveColor(true);
            }

            else
            {
                deviceUses[i].deviceSelector.SetSensorActiveColor(false);
            }

            // both

            if (deviceUses[i].displayOK && deviceUses[i].kinectOK)
            {
                deviceUses[i].deviceSelector.SetSetupCompleteColor(true);
            }
            else
            {
                deviceUses[i].deviceSelector.SetSetupCompleteColor(false);
            }
        }
    }


}



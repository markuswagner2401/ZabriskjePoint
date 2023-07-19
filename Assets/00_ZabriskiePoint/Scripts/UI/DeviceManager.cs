using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using TMPro;
using UnityEngine.SceneManagement;

public class DeviceManager : MonoBehaviour
{
    [System.Serializable]
    public struct DeviceUse
    {
        public string useName;


        public DeviceSelector deviceSelector;
        public KinectManager kinectManager;
        public Kinect4AzureInterface kinect4AzureInterface;

        public float minDepthDistance;

        public float maxDepthDistance;
        public Camera camera;

        public bool displayOK;

        public bool kinectOK;

    }
    [SerializeField] DeviceUse[] deviceUses;

    int displayCount;



    List<Camera> activeCameras = new List<Camera>();

    private void Start()
    {
        Initialize();
    }

    

    public void Initialize()
    {
        DeactivateUnused();
        UpdateDisplayCameraPatch();
        SetupKinect();

    }

    void DeactivateUnused()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            if (deviceUses[i].camera == null)
            {
                deviceUses[i].deviceSelector.gameObject.SetActive(false);
            }
        }
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
            if (deviceUses[i].camera == null) continue;

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

    public void ActivateKinects(bool value)
    {
        if (!value)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            // StopKinects();
            // InitializeDepthTextureProvider(false);
        }

        else
        {
            StartCoroutine(SetupKinectsRoutine());
        }
    }

    IEnumerator SetupKinectsRoutine()
    {

        SetupKinect();

        yield return new WaitForSeconds(0.2f);

        StartKinects();

        yield return new WaitForSeconds(1f);

        InitializeDepthTextureProvider(true);

        yield return new WaitForSeconds(0.2f);

        UpdateValidColors();

        yield break;

    }



    private void SetupKinect()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            if (deviceUses[i].camera == null || deviceUses[i].kinect4AzureInterface == null) continue;

            int kinectIndex = deviceUses[i].deviceSelector.GetSensorDropdownValue();



            Debug.Log("Setting Sensor Index for " + deviceUses[i].useName + " to: " + kinectIndex);

            List<KinectInterop.SensorDeviceInfo> alSensors = deviceUses[i].kinect4AzureInterface.GetAvailableSensors();
            MyConsole.Instance.Print("Vefügbare Sensoren (Kinects): " + alSensors.Count);
            if (kinectIndex >= alSensors.Count)
            {
                deviceUses[i].kinectOK = false;
            }
            else
            {
                deviceUses[i].kinectOK = true;
                deviceUses[i].kinect4AzureInterface.deviceIndex = kinectIndex;
                deviceUses[i].kinect4AzureInterface.minDepthDistance = deviceUses[i].minDepthDistance;
                deviceUses[i].kinect4AzureInterface.maxDepthDistance = deviceUses[i].maxDepthDistance;


            }
        }


    }

    private void UpdateValidColors()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            if (deviceUses[i].camera == null) continue;

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

    private void StartKinects()
    {
        KinectManager.Instance.StartDepthSensors();
    }

    public void StopKinects()
    {
        KinectManager.Instance.StopDepthSensors();
    }

    private void InitializeDepthTextureProvider(bool value)
    {
        ColorCamDepthTextureProvider[] depthTextureProviders = FindObjectsByType<ColorCamDepthTextureProvider>(FindObjectsSortMode.None);
        foreach (var item in depthTextureProviders)
        {
            if (value)
            {
                item.Initialize();
            }
            else
            {
                item.Deinitialize();
            }

        }
    }


}



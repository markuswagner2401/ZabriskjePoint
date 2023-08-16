using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using com.rfilkov.components;

public class DeviceManager : MonoBehaviour
{
    //private static DeviceManager _instance;

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

    [SerializeField] UnityEvent doOnKinectStarted;

    public delegate void DelegateOnKinectStarted();

    //public DelegateOnKinectStarted delegateOnKinectStarted;

    int displayCount;

    bool kinectsStarted = false;


    List<Camera> activeCameras = new List<Camera>();

    [SerializeField] bool activateAtStart = false;

    //public Kinect4AzureInterface generalKinect4AzureInterface;



    //private void Awake()
    //{
    //    if (_instance != null && _instance != this)
    //    {
    //        Destroy(this.gameObject);
    //    }
    //    else
    //    {
    //        _instance = this;
    //        DontDestroyOnLoad(this.gameObject);
    //    }
    //}


    private void Start()
    {
        
        Initialize();
        
    }

    private void Update() 
    {
        
        
    }

    //public static DeviceManager Instance
    //{
    //    get
    //    {
    //        return _instance;
    //    }
    //}



    public void Initialize()
    {
        DeactivateUnused();
        UpdateDisplayCameraPatch();
        if(activateAtStart)
        {
            ActivateKinects(true);
        }
        else
        {
            SetupKinect();
        }
        

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
            if(kinectsStarted)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            

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

        yield return WaitUntilKinectInitializedR();

        

        InitializeDepthTextureProvider(true);

        yield return new WaitForSeconds(0.2f);

        UpdateValidColors();

        doOnKinectStarted.Invoke();

        //delegateOnKinectStarted.Invoke();

        //KinectReactivator.Instance?.StartKinectReactivator();

        

        //StartCoroutine(CheckKinectsRoutine());

        yield break;

    }



    private void SetupKinect()
    {
        for (int i = 0; i < deviceUses.Length; i++)
        {
            if(deviceUses[i].kinect4AzureInterface == null)
            {
                deviceUses[i].kinectOK = true;
            }
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

    // checking

    //IEnumerator CheckKinectsRoutine()
    //{
    //    bool kinectsWorking = true;
    //    while (kinectsStarted && kinectsWorking)
    //    {
    //        kinectsWorking = CheckKinects();
    //        print("check kinects");
    //        yield return new WaitForSeconds(1f);
    //    }

    //    if(kinectsStarted && !kinectsWorking) 
    //    {
    //        print("deactivate kinects on broken connection");
    //        ActivateKinects(false);
    //        //StopKinects();
    //        StartCoroutine(TryReactivateKinectsR());
    //    }



    //}

    //IEnumerator TryReactivateKinectsR()
    //{
    //    while (kinectsStarted && !CheckKinects())
    //    {
            
    //        print("Wait for available sensors to reactivate");
    //        yield return new WaitForSeconds(3f);
    //    }
    //    print("reactite kinects");

    //    ActivateKinects(true);

    //    yield break;
    //}

    //private bool CheckKinects()
    //{

        //List<KinectInterop.SensorDeviceInfo> alSensors = generalKinect4AzureInterface.GetAvailableSensors();

        //if (alSensors.Count == 0) { return false; } else { return true; }

        

        //for (int i = 0; i < deviceUses.Length; i++)
        //{
        //    if (deviceUses[i].kinect4AzureInterface == null) continue;

        //    int kinectIndex = deviceUses[i].deviceSelector.GetSensorDropdownValue();

        //    List<KinectInterop.SensorDeviceInfo> alSensors = deviceUses[i].kinect4AzureInterface.GetAvailableSensors();
            
        //    if (kinectIndex >= alSensors.Count)
        //    {
        //        deviceUses[i].kinectOK = false;
        //        return false;
        //    }
        //    else
        //    {
        //        deviceUses[i].kinectOK = true;
        //        return true;
  
        //    }
        //}
        //return false;

    //}

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

        kinectsStarted = true;
    }

    IEnumerator WaitUntilKinectInitializedR()
    {
        while (!KinectManager.Instance.IsInitialized())
        {
            yield return null;
        }

        yield break;
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

        ColorCamUserImageProvider[] userImageProviders = FindObjectsOfType<ColorCamUserImageProvider>();

        foreach (var item in userImageProviders)
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



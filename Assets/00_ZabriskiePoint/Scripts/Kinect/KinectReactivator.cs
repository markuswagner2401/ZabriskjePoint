using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;

public class KinectReactivator : MonoBehaviour
{
    private static KinectReactivator _instance;

    Kinect4AzureInterface generalKinect4AzureInterface = null;

    bool reactivationLoopRunning = false;






    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public static KinectReactivator Instance
    {
        get
        {
            return _instance;
        }
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    public void StartKinectReactivator()
    {
        if (reactivationLoopRunning) return;
        reactivationLoopRunning = true;
        StartCoroutine(CheckKinectsRoutine());
    }

    IEnumerator CheckKinectsRoutine()
    {
        bool kinectsWorking = true;
        while (kinectsWorking)
        {
            if (generalKinect4AzureInterface == null)
            {
                generalKinect4AzureInterface = FindAnyObjectByType<Kinect4AzureInterface>();
                yield return null;
            }

            kinectsWorking = CheckKinects();
            print("check kinects");
            yield return new WaitForSeconds(1f);
        }

        if (!kinectsWorking)
        {
            print("deactivate kinects on broken connection");
            FindAnyObjectByType<DeviceManager>().ActivateKinects(false);
            //StopKinects();
            StartCoroutine(TryReactivateKinectsR());
        }



    }

    IEnumerator TryReactivateKinectsR()
    {
        bool kinectsWorking = false;
        while (!kinectsWorking)
        {
            if(generalKinect4AzureInterface == null)
            {
                generalKinect4AzureInterface = FindAnyObjectByType<Kinect4AzureInterface>();
                yield return null;
            }
            kinectsWorking = CheckKinects();
            print("Wait for available sensors to reactivate");
            yield return new WaitForSeconds(1f);
        }
        print("reactivate kinects");

        yield return new WaitForSeconds(1f);
        

        FindAnyObjectByType<DeviceManager>().ActivateKinects(true);

        StartCoroutine(CheckKinectsRoutine());

        yield break;
    }

    private bool CheckKinects()
    {
        

        List<KinectInterop.SensorDeviceInfo> alSensors = generalKinect4AzureInterface.GetAvailableSensors();

        if (alSensors.Count == 0) { return false; } else { return true; }
    }
}

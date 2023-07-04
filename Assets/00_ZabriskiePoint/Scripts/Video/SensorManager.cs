using System.Collections;
using System.Collections.Generic;
using com.rfilkov.kinect;
using UnityEngine;

public class SensorManager : MonoBehaviour
{
    


    
    [SerializeField] SensorUse[] sensorUses;

    [System.Serializable]
    public struct SensorUse
    {
        public string name;
        public KinectManager kinectManager;
        public Kinect4AzureInterface kinect4AzureInterface;
        public DeviceSelector selector;
        

        // parameters for sensor uses like min max distance are defined in corresponding cinect manager and kinect4azureInterface
    }


    

    int connectedSensors;
    
    void Start()
    {

    }

    void SetupSensorUses()
    {
        // find connected sensors

        connectedSensors = sensorUses[0].kinect4AzureInterface.GetAvailableSensors().Count;

        for (int i = 0; i < sensorUses.Length; i++)
        {
            
        }

    }


        
}

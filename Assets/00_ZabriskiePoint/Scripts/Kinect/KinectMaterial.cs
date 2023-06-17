using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;

public class KinectMaterial : MonoBehaviour
{
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    [SerializeField] Material material;
    [SerializeField] string textureRef;

    Texture kinectTexture;
    void Start()
    {
        kinectManager = KinectManager.Instance;
        sensorData = kinectManager != null ? kinectManager.GetSensorData(sensorIndex) : null;
    }


    void Update()
    {
        if (kinectManager && kinectManager.IsInitialized())
        {
            kinectTexture = kinectManager.GetDepthImageTex(sensorIndex);
        }

        material.SetTexture(textureRef, kinectTexture);
    }
}

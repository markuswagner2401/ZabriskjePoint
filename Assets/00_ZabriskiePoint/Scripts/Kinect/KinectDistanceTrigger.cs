using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using System;

public class KinectDistanceTrigger : MonoBehaviour
{
    public int sensorIndex = 0;

    public int depthThreshold = 100;

    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    void Start()
    {
        kinectManager = KinectManager.Instance;
        sensorData = kinectManager != null ? kinectManager.GetSensorData(sensorIndex) : null;

    }

    // Update is called once per frame
    void Update()
    {
        if (kinectManager && kinectManager.IsInitialized() && sensorData != null)
        {
            if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamDepthImage == null)
                return;
            int frameLen = sensorData.colorCamDepthImage.Length;
            for (int i = 0; i < frameLen; i++)
            {
                int depth = sensorData.colorCamDepthImage[i];
                int limDepth = (depth <= DepthSensorBase.MAX_DEPTH_DISTANCE_MM) ? depth : 0;

                if(limDepth > 0 && limDepth < depthThreshold)
                {
                    //print("Dist Threshold broken, depht =: " + limDepth);
                    return;
                }

                // if (limDepth > 0)
                // {
                //     depthHistBufferData[limDepth]++;
                //     depthHistTotalPoints++;
                // }
            }




        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using System;
using UnityEngine.Events;
using UnityEngine.VFX;

public class ColorCamDepthTextureProvider_Avatar : MonoBehaviour
{
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    [SerializeField] Material receiverMaterial;

    

    public String depthTextureRef = "_Texture";

    public String colorTextureRef = "_ColorTexture";

    public String userImageRef = "_UserImage";

    bool colorTextureSet = false;





    // last camera rect width & height
    private float lastCamRectW = 0;
    private float lastCamRectH = 0;

    // reference to the kinectManager
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private Vector2 initialAnchorPos = Vector2.zero;

    // color-camera aligned frames
    private ulong lastColorCamDepthFrameTime = 0;

    // color-camera aligned texture and buffers
    private RenderTexture depthImageTexture = null;
    private Material depthImageMaterial = null;

    private ComputeBuffer depthImageBuffer = null;
    private ComputeBuffer depthHistBuffer = null;

    // depth image hist data
    protected int[] depthHistBufferData = null;
    protected int[] equalHistBufferData = null;
    protected int depthHistTotalPoints = 0;

    bool closestPointTriggerRunning = false;

    ushort[] currentDepthImage;

    ushort[] previousDepthImage;

    int frameLen;

    int rawDepthImageLength;


    void Start()
    {
        Initialize();

    }

    public void Initialize()
    {
        

        kinectManager = KinectManager.Instance;
        sensorData = kinectManager != null ? kinectManager.GetSensorData(sensorIndex) : null;

        if (sensorData != null)
        {
            print("sensor data != null");
            // enable the color camera aligned depth frames 
            sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, true);

            // create the output texture and needed buffers
            depthImageTexture = KinectInterop.CreateRenderTexture(depthImageTexture, sensorData.colorImageWidth, sensorData.colorImageHeight);
            depthImageMaterial = new Material(Shader.Find("Kinect/DepthHistImageShaderSW"));

            //int depthBufferLength = sensorData.colorImageWidth * sensorData.colorImageHeight >> 1;
            //depthImageBuffer = KinectInterop.CreateComputeBuffer(depthImageBuffer, depthBufferLength, sizeof(uint));

            depthHistBuffer = KinectInterop.CreateComputeBuffer(depthHistBuffer, DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1, sizeof(int));

            depthHistBufferData = new int[DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1];
            equalHistBufferData = new int[DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1];






        }

        if (depthImageTexture != null)
        {
            if (receiverMaterial != null)
            {
                receiverMaterial.SetTexture(depthTextureRef, depthImageTexture);
                print("Set Texture at receiver material");
            }

           
        }





        frameLen = sensorData.colorCamDepthImage.Length;

        currentDepthImage = new ushort[frameLen];

        previousDepthImage = new ushort[frameLen];

        // depths check compute shader 




       

    }

    public void Deinitialize()
    {
        StopAllCoroutines();

        if (depthImageTexture)
        {
            if (receiverMaterial != null)
            {
                receiverMaterial.SetTexture(depthTextureRef, null);
                receiverMaterial.SetTexture(colorTextureRef, null);
                print("Release Texture at receiver material");
            }

           

            depthImageTexture.Release();
            depthImageTexture = null;
        }

        if (depthImageBuffer != null)
        {
            depthImageBuffer.Dispose();
            depthImageBuffer = null;
        }

        if (depthHistBuffer != null)
        {
            depthHistBuffer.Dispose();
            depthHistBuffer = null;
        }

        if (sensorData != null)
        {
            // disable the color camera aligned depth frames 
            sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, false);
        }

      



        colorTextureSet = false;

   

    }


    void OnDestroy()
    {
        if (depthImageTexture)
        {
            depthImageTexture.Release();
            depthImageTexture = null;
        }

        if (depthImageBuffer != null)
        {
            depthImageBuffer.Dispose();
            depthImageBuffer = null;
        }

        if (depthHistBuffer != null)
        {
            depthHistBuffer.Dispose();
            depthHistBuffer = null;
        }

        if (sensorData != null)
        {
            // disable the color camera aligned depth frames 
            sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, false);
        }

        //lookupPixelSegment.Clear();

        
    }




    void Update()
    {
        if (kinectManager && kinectManager.IsInitialized() && sensorData != null)
        {

            UpdateTextureWithNewFrame();

           


            // for debugging, disable later to safe performance
            if (!colorTextureSet)
            {
                if (receiverMaterial != null)
                {
                    receiverMaterial.SetTexture(colorTextureRef, kinectManager.GetColorImageTex(sensorIndex));
                    receiverMaterial.SetTexture(userImageRef, kinectManager.GetUsersImageTex(0));
                    
                }
                
                colorTextureSet = true;
            }

        }

        



    }

    

    // checks for new color-camera aligned frames, and composes an updated body-index texture, if needed
    private void UpdateTextureWithNewFrame()
    {
        if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamDepthImage == null)
            return;

        if (depthImageTexture == null) return;

        //print("update texture with new frame");



        // get the updated depth frame
        if (lastColorCamDepthFrameTime != sensorData.lastColorCamDepthFrameTime)
        {


            lastColorCamDepthFrameTime = sensorData.lastColorCamDepthFrameTime;


            if (depthImageTexture.width != sensorData.colorImageWidth || depthImageTexture.height != sensorData.colorImageHeight)
            {
                depthImageTexture = KinectInterop.CreateRenderTexture(depthImageTexture, sensorData.colorImageWidth, sensorData.colorImageHeight);
            }

            Array.Clear(depthHistBufferData, 0, depthHistBufferData.Length);
            Array.Clear(equalHistBufferData, 0, equalHistBufferData.Length);
            depthHistTotalPoints = 0;

            // get configured min & max distances 
            float minDistance = ((DepthSensorBase)sensorData.sensorInterface).minDepthDistance;
            float maxDistance = ((DepthSensorBase)sensorData.sensorInterface).maxDepthDistance;

            int depthMinDistance = (int)(minDistance * 1000f);
            int depthMaxDistance = (int)(maxDistance * 1000f);

            int frameLen = sensorData.colorCamDepthImage.Length;





            for (int i = 0; i < frameLen; i++)
            {
                int depth = sensorData.colorCamDepthImage[i];
                int limDepth = (depth <= DepthSensorBase.MAX_DEPTH_DISTANCE_MM) ? depth : 0;



                if (limDepth > 0)
                {
                    depthHistBufferData[limDepth]++;
                    depthHistTotalPoints++;


                }
            }





            equalHistBufferData[0] = depthHistBufferData[0];
            for (int i = 1; i < depthHistBufferData.Length; i++)
            {
                equalHistBufferData[i] = equalHistBufferData[i - 1] + depthHistBufferData[i];
            }

            // make depth 0 equal to the max-depth
            equalHistBufferData[0] = equalHistBufferData[equalHistBufferData.Length - 1];

            int depthBufferLength = sensorData.colorCamDepthImage.Length >> 1;
            if (depthImageBuffer == null || depthImageBuffer.count != depthBufferLength)
            {
                depthImageBuffer = KinectInterop.CreateComputeBuffer(depthImageBuffer, depthBufferLength, sizeof(uint));
            }

            KinectInterop.SetComputeBufferData(depthImageBuffer, sensorData.colorCamDepthImage, depthBufferLength, sizeof(uint));

            if (depthHistBuffer != null)
            {
                KinectInterop.SetComputeBufferData(depthHistBuffer, equalHistBufferData, equalHistBufferData.Length, sizeof(int));
            }

            depthImageMaterial.SetInt("_TexResX", sensorData.colorImageWidth);
            depthImageMaterial.SetInt("_TexResY", sensorData.colorImageHeight);
            depthImageMaterial.SetInt("_MinDepth", depthMinDistance);
            depthImageMaterial.SetInt("_MaxDepth", depthMaxDistance);

            depthImageMaterial.SetBuffer("_DepthMap", depthImageBuffer);
            depthImageMaterial.SetBuffer("_HistMap", depthHistBuffer);
            depthImageMaterial.SetInt("_TotalPoints", depthHistTotalPoints);



            

            Graphics.Blit(null, depthImageTexture, depthImageMaterial);


        }


    }

    
}



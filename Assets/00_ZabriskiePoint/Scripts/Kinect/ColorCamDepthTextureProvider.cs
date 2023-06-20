using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using System;
using UnityEngine.Events;

public class ColorCamDepthTextureProvider : MonoBehaviour
{
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    // Trigger




    int nearestPointDistance = 9999999;

    int lastNearestPointDistance;

    bool nearestDistanceChanging = false;

    [SerializeField] UnityEvent onDepthChangingEnd;

    [SerializeField] UnityEvent onDepthChangingStart;

    [SerializeField] float triggerDelayIn = 1f;
    [SerializeField] float triggerDelayOut = 1f;

    [Range(0, 1)]
    [SerializeField] float changingBar = 0;






    bool changingEventTriggered = false;

    bool changingEndEventTriggered = false;


    public int distChangeThreshold = 10;

    public float distanceChecckFrequency = 0.1f;

    //

    Dictionary<int, int> lookupPixelSegment = new Dictionary<int, int>();

    int[] closestPointsInSegments;



    int[] lastClosestPointsInSegments;

    [SerializeField] int heightSegments = 2;
    [SerializeField] int widthSegments = 2;

    ////




    public Material receiverMaterial;

    public String depthTextureRef = "_Texture";

    public String colorTextureRef = "_ColorTexture";





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


    void Start()
    {
        if (receiverMaterial == null)
        {
            receiverMaterial = GetComponent<Material>();
        }

        kinectManager = KinectManager.Instance;
        sensorData = kinectManager != null ? kinectManager.GetSensorData(sensorIndex) : null;

        if (sensorData != null)
        {
            // enable the color camera aligned depth frames 
            sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, true);

            // create the output texture and needed buffers
            depthImageTexture = KinectInterop.CreateRenderTexture(depthImageTexture, sensorData.colorImageWidth, sensorData.colorImageHeight);
            depthImageMaterial = new Material(Shader.Find("Kinect/DepthHistImageShader"));

            //int depthBufferLength = sensorData.colorImageWidth * sensorData.colorImageHeight >> 1;
            //depthImageBuffer = KinectInterop.CreateComputeBuffer(depthImageBuffer, depthBufferLength, sizeof(uint));

            depthHistBuffer = KinectInterop.CreateComputeBuffer(depthHistBuffer, DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1, sizeof(int));

            depthHistBufferData = new int[DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1];
            equalHistBufferData = new int[DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1];

            /// Trigger





        }

        if (receiverMaterial && depthImageTexture != null)
        {
            receiverMaterial.SetTexture(depthTextureRef, depthImageTexture);
        }




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
    }




    void Update()
    {
        if (kinectManager && kinectManager.IsInitialized() && sensorData != null)
        {
            UpdateTextureWithNewFrame();

            if (!closestPointTriggerRunning)
            {
                StartCoroutine(ClosestPointTriggerRoutine());
                closestPointTriggerRunning = true;
            }



            receiverMaterial.SetTexture(colorTextureRef, kinectManager.GetColorImageTex(sensorIndex));
        }

        if (changingBar > 0.1f)
        {
            changingEndEventTriggered = false;
            if (!changingEventTriggered)
            {
                changingEventTriggered = true;
                onDepthChangingStart.Invoke();
                // print("trigger changing");
            }
        }

        if (changingBar < 0.1f)
        {
            changingEventTriggered = false;
            if (!changingEndEventTriggered)
            {
                changingEndEventTriggered = true;
                onDepthChangingEnd.Invoke();
                //print("trigger changing end");
            }
        }



    }

    IEnumerator ClosestPointTriggerRoutine()
    {
        CreatePixelSegmentLookup(depthImageTexture.width, depthImageTexture.height);

  

        closestPointsInSegments = new int[heightSegments * widthSegments];

        lastClosestPointsInSegments = new int[heightSegments * widthSegments];


        while (true)
        {
            if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamDepthImage == null)
                yield return null;

            int frameLen = sensorData.colorCamDepthImage.Length;

            for (int i = 0; i < closestPointsInSegments.Length; i++)
            {
                closestPointsInSegments[i] = 100000;
            }

            nearestDistanceChanging = false;

            for (int i = 0; i < frameLen; i++)
            {
                int depth = sensorData.colorCamDepthImage[i];
                int limDepth = (depth <= DepthSensorBase.MAX_DEPTH_DISTANCE_MM) ? depth : 0;

                int currentTriggerSegment = lookupPixelSegment[i];

                if (limDepth > 0)
                {
                    if (limDepth < closestPointsInSegments[currentTriggerSegment])
                    {
                        closestPointsInSegments[currentTriggerSegment] = limDepth;
                    }

                }


            }

            for (int j = 0; j < closestPointsInSegments.Length; j++)
            {

                if (Mathf.Abs(closestPointsInSegments[j] - lastClosestPointsInSegments[j]) > distChangeThreshold)
                {
                    nearestDistanceChanging = true;
                    lastClosestPointsInSegments[j] = closestPointsInSegments[j];
                    //print("Segment Changed: " +  j + " closest point: " + closestPointsInSegments[j]);
                }

            }

            if (nearestDistanceChanging)
            {
                changingBar = Mathf.Lerp(changingBar, 1f, distanceChecckFrequency * triggerDelayIn);
            }

            else
            {
                changingBar = Mathf.Lerp(changingBar, 0f, distanceChecckFrequency * triggerDelayOut);
            }

            yield return new WaitForSeconds(distanceChecckFrequency);


        }




    }


    // checks for new color-camera aligned frames, and composes an updated body-index texture, if needed
    private void UpdateTextureWithNewFrame()
    {
        if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamDepthImage == null)
            return;


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



            if (nearestDistanceChanging)
            {

                Graphics.Blit(null, depthImageTexture, depthImageMaterial);
            }


        }


    }

    private void CreatePixelSegmentLookup(int imageWidth, int imageHeight)
    {

        int segmentHeight = imageHeight / heightSegments; // height of each segment
        int segmentWidth = imageWidth / widthSegments; // width of each segment

        // Check if the image height and width are evenly divisible by the number of segments
        if (imageHeight % heightSegments != 0 || imageWidth % widthSegments != 0)
        {
            Debug.LogError("The image dimensions are not evenly divisible by the number of segments. This can cause Problems");
        }

        int totalPixels = imageHeight * imageWidth;
        for (int i = 0; i < totalPixels; i++)
        {
            int row = i / imageWidth; // calculate row of the pixel
            int col = i % imageWidth; // calculate column of the pixel

            int segmentRow = row / segmentHeight; // calculate segment row
            int segmentCol = col / segmentWidth; // calculate segment column

            // calculate unique segment number
            int segmentNumber = segmentRow * widthSegments + segmentCol;

            lookupPixelSegment.Add(i, segmentNumber);
        }
    }



    int[] CreateClosestPointsStartArray()
    {
        int[] newArray = new int[heightSegments * widthSegments];
        for (int i = 0; i < newArray.Length; i++)
        {
            newArray[i] = 10000;
        }
        return newArray;
    }
}



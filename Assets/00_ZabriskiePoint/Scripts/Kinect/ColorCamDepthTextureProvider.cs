using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;
using System;
using UnityEngine.Events;
using UnityEngine.VFX;

public class ColorCamDepthTextureProvider : MonoBehaviour
{
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    // Trigger

    // DephsCheck

    public ComputeShader depthCheckShader;

    private int depthCheckKernel;
    private ComputeBuffer currentDepthBuffer;
    private ComputeBuffer prevDepthBuffer;
    private ComputeBuffer changeDataBuffer;
    private int[] currentDepthData;

    private int currentColorCamPixel;
    private int[] prevDepthData;
    private int[] changeData;

    [SerializeField] int pixelChangeThreshold = 100;



    [SerializeField] RenderTexture depthChangeDebug = null;

    RenderTexture writableDepthChangeDebug;

    //

    public struct SegmentInfo
    {
        public int ClosestLength;
        public int PixelIndex;
    }


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

    SegmentInfo[] closestPointsInSegments;

    SegmentInfo[] lastClosestPointsInSegments;

    [SerializeField] int heightSegments = 2;
    [SerializeField] int widthSegments = 2;

    Vector2 changingPosition = new Vector2();

    [Tooltip("Set null to disable")]
    [SerializeField] MaterialPropertiesFader_2 materialPropertiesFader_2 = null;

    [Tooltip("Set null to disable")]
    [SerializeField] VFXParameterAnimator vFXParameterAnimator = null;

    ////



    [Tooltip("Set null to disable")]
    public Material receiverMaterial = null;

    [Tooltip("Set null to disable")]
    public VisualEffect vfx = null;

    public String depthTextureRef = "_Texture";

    public String colorTextureRef = "_ColorTexture";

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
        //Initialize();

        // if (materialPropertiesFader_2 == null)
        // {
        //     materialPropertiesFader_2 = GetComponent<MaterialPropertiesFader_2>();
        // }

    }

    public void Initialize()
    {
        //sensorIndex = currentSensorIndex;
        // if (receiverMaterial == null)
        // {
        //     receiverMaterial = GetComponent<Material>();
        // }

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

            if (vfx != null)
            {
                vfx.SetTexture(depthTextureRef, depthImageTexture);
                print("depth image texture resolution: " + depthImageTexture.width + " " + depthImageTexture.height);
                vfx.Reinit();
            }
        }





        frameLen = sensorData.colorCamDepthImage.Length;

        currentDepthImage = new ushort[frameLen];

        previousDepthImage = new ushort[frameLen];

        // depths check compute shader 




        //Initializing
        depthCheckKernel = depthCheckShader.FindKernel("CheckDepthChange");



        currentDepthData = new int[frameLen];
        prevDepthData = new int[frameLen];
        changeData = new int[frameLen];



        currentDepthBuffer = new ComputeBuffer(frameLen, sizeof(int));
        prevDepthBuffer = new ComputeBuffer(frameLen, sizeof(int));
        changeDataBuffer = new ComputeBuffer(frameLen, sizeof(int));


        writableDepthChangeDebug = new RenderTexture(depthImageTexture.width, depthImageTexture.height, 24);
        writableDepthChangeDebug.enableRandomWrite = true;
        writableDepthChangeDebug.Create();

        // Initializing

        // depthCheckKernel = depthCheckShader.FindKernel("CheckDepthChange");

        // rawDepthImageLength = sensorData.depthImage.Length;

        // currentDepthData = new int[rawDepthImageLength];
        // prevDepthData = new int[rawDepthImageLength];
        // changeData = new int[rawDepthImageLength];




        // currentDepthBuffer = new ComputeBuffer(rawDepthImageLength, sizeof(int));
        // prevDepthBuffer = new ComputeBuffer(rawDepthImageLength, sizeof(int));
        // changeDataBuffer = new ComputeBuffer(rawDepthImageLength, sizeof(int));


        // writableDepthChangeDebug = new RenderTexture(depthImageTexture.width, depthImageTexture.height, 24);
        // writableDepthChangeDebug.enableRandomWrite = true;
        // writableDepthChangeDebug.Create();


        ///

        CreatePixelSegmentLookup(depthImageTexture.width, depthImageTexture.height);

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

            if (vfx != null)
            {
                vfx.SetTexture(depthTextureRef, null);

                print("Release Texture at VFX");

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

        lookupPixelSegment.Clear();

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

        lookupPixelSegment.Clear();

        currentDepthBuffer?.Release();
        prevDepthBuffer?.Release();
        changeDataBuffer?.Release();
    }




    void Update()
    {
        if (kinectManager && kinectManager.IsInitialized() && sensorData != null)
        {

            UpdateTextureWithNewFrame();

            if (!closestPointTriggerRunning)
            {
                //StartCoroutine(ClosestPointTriggerRoutine()); old
                StartCoroutine(DepthsCheckRoutine()); // new with compute shader
                closestPointTriggerRunning = true;
            }


            // for debugging, disable later to safe performance
            if (!colorTextureSet)
            {
                if (receiverMaterial != null)
                {
                    receiverMaterial.SetTexture(colorTextureRef, kinectManager.GetColorImageTex(sensorIndex));
                }

                if (vfx != null)
                {
                    vfx.SetTexture(colorTextureRef, kinectManager.GetColorImageTex(sensorIndex));
                }

                colorTextureSet = true;
            }

        }

        if (nearestDistanceChanging)
        {
            changingBar = Mathf.Lerp(changingBar, 1f, Time.deltaTime * triggerDelayIn);
        }

        else
        {
            changingBar = Mathf.Lerp(changingBar, 0f, Time.deltaTime * triggerDelayOut);
        }

        if (changingBar > 0.1f)
        {
            changingEndEventTriggered = false;
            if (!changingEventTriggered)
            {
                if (gameObject.activeInHierarchy)
                {
                    changingEventTriggered = true;
                    onDepthChangingStart.Invoke();
                    print("trigger changing");
                }

            }
        }

        if (changingBar < 0.1f)
        {
            changingEventTriggered = false;
            if (!changingEndEventTriggered)
            {
                if (gameObject.activeInHierarchy)
                {
                    changingEndEventTriggered = true;
                    onDepthChangingEnd.Invoke();
                }

                print("trigger changing end");
            }
        }

        if (changingPosition != Vector2.zero)
        {
            if (materialPropertiesFader_2 != null && materialPropertiesFader_2.gameObject.activeInHierarchy)
            {
                materialPropertiesFader_2?.UpdateFloat(0, changingPosition.x);
                materialPropertiesFader_2?.UpdateFloat(1, changingPosition.y);
            }

            if (vFXParameterAnimator != null && vFXParameterAnimator.gameObject.activeInHierarchy)
            {
                vFXParameterAnimator?.UpdateFloat(0, changingPosition.x);
                vFXParameterAnimator?.UpdateFloat(1, changingPosition.y);
            }

        }



    }

    IEnumerator ClosestPointTriggerRoutine()
    {
        //CreatePixelSegmentLookup(depthImageTexture.width, depthImageTexture.height);



        SegmentInfo[] closestPointsInSegments = new SegmentInfo[heightSegments * widthSegments];
        SegmentInfo[] lastClosestPointsInSegments = new SegmentInfo[heightSegments * widthSegments];



        while (true)
        {
            if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamDepthImage == null)
                yield return null;

            //int frameLen = sensorData.colorCamDepthImage.Length;

            // local copy for consistency

            // int[] localDepthImage = new int[frameLen];
            // Array.Copy(sensorData.colorCamDepthImage, localDepthImage, frameLen);

            //

            //currentDepthImage = sensorData.colorCamDepthImage;



            Array.Copy(sensorData.colorCamDepthImage, currentDepthImage, sensorData.colorCamDepthImage.Length);

            for (int i = 0; i < closestPointsInSegments.Length; i++)
            {
                closestPointsInSegments[i] = new SegmentInfo { ClosestLength = 100000, PixelIndex = closestPointsInSegments[i].PixelIndex };
            }

            nearestDistanceChanging = false;
            changingPosition = Vector2.zero;

            for (int i = 0; i < frameLen; i++)
            {
                //int depth = sensorData.colorCamDepthImage[i];

                int depth = currentDepthImage[i];

                int limDepth = (depth <= DepthSensorBase.MAX_DEPTH_DISTANCE_MM) ? depth : 0;

                int currentTriggerSegment = lookupPixelSegment[i];

                if (limDepth > 0 && limDepth < closestPointsInSegments[currentTriggerSegment].ClosestLength)
                {
                    closestPointsInSegments[currentTriggerSegment] = new SegmentInfo { ClosestLength = limDepth, PixelIndex = i };
                }
            }

            for (int j = 0; j < closestPointsInSegments.Length; j++)
            {
                if (Mathf.Abs(closestPointsInSegments[j].ClosestLength - lastClosestPointsInSegments[j].ClosestLength) > distChangeThreshold)
                {
                    nearestDistanceChanging = true;
                    lastClosestPointsInSegments[j] = closestPointsInSegments[j];

                    int x = closestPointsInSegments[j].PixelIndex % depthImageTexture.width; // column index
                    int y = closestPointsInSegments[j].PixelIndex / depthImageTexture.width; // row index

                    float uvX = (float)x / (float)depthImageTexture.width;
                    float uvY = (float)y / (float)depthImageTexture.height;



                    changingPosition = new Vector2(uvX, uvY);

                    //print("Changing position: " + changingPosition);


                }
            }



            yield return new WaitForSeconds(distanceChecckFrequency);


        }




    }

    // compute shader way
    IEnumerator DepthsCheckRoutine()
    {
        while (true)
        {
            if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamDepthImage == null)
            {
                yield return null;
            }

            // Copying data
            for (int i = 0; i < sensorData.colorCamDepthImage.Length; i++)
            {
                currentDepthData[i] = sensorData.colorCamDepthImage[i];
            }



            currentDepthBuffer.SetData(currentDepthData);
            prevDepthBuffer.SetData(prevDepthData);
            changeDataBuffer.SetData(changeData);


            depthCheckShader.SetBuffer(depthCheckKernel, "currentDepthData", currentDepthBuffer);
            depthCheckShader.SetBuffer(depthCheckKernel, "prevDepthData", prevDepthBuffer);
            depthCheckShader.SetBuffer(depthCheckKernel, "changeData", changeDataBuffer);
            depthCheckShader.SetInt("depthImageWidth", depthImageTexture.width);
            depthCheckShader.SetInt("maxDepthDistance", DepthSensorBase.MAX_DEPTH_DISTANCE_MM);

            depthCheckShader.SetTexture(depthCheckKernel, "changePointsDebug", writableDepthChangeDebug);

            //depthCheckShader.Dispatch(depthCheckKernel, Mathf.CeilToInt((float)currentDepthData.Length * 2 / 256), 1, 1);
            depthCheckShader.Dispatch(depthCheckKernel, Mathf.CeilToInt((float)currentDepthData.Length / 256), 1, 1);

            changeDataBuffer.GetData(changeData);
            if (depthChangeDebug.width == writableDepthChangeDebug.width && depthChangeDebug.height == writableDepthChangeDebug.height)
            {
                Graphics.Blit(writableDepthChangeDebug, depthChangeDebug);
            }
            else
            {
                Debug.LogError("Rendertexture Resolution mismatch: depthChangeDebug and writableDepthChangeDebug");
            }


            // TODO: Check changeData array for any changes and perform necessary actions.
            nearestDistanceChanging = false;
            int sumPosX = 0;
            int sumPosY = 0;
            int changingPixels = 0;
            for (int i = 0; i < changeData.Length; i++)
            {
                int limDepth = (changeData[i] <= DepthSensorBase.MAX_DEPTH_DISTANCE_MM) ? changeData[i] : 0;
                if (limDepth == 1)
                {
                    // print("change happened");
                    //     break;

                    changingPixels += 1;
                    if (changingPixels > pixelChangeThreshold)
                    {
                        nearestDistanceChanging = true;
                        //break;
                    }
                    sumPosX += (i % depthImageTexture.width);
                    sumPosY += (i / depthImageTexture.width);
                }
            }

            if(nearestDistanceChanging)
            {
                
                float uvX = (float)(sumPosX / changingPixels) / (float)depthImageTexture.width;
                float uvY = (float)(sumPosY / changingPixels) / (float)depthImageTexture.height;

                changingPosition.x = uvX;
                changingPosition.y = uvY;
                //print("change Happened: " + changingPosition);
            }

            else
            {
                changingPosition = Vector3.zero;
            }

            // Set current depth data as previous depth data for the next frame.
            System.Array.Copy(currentDepthData, prevDepthData, currentDepthData.Length);

            yield return new WaitForSeconds(distanceChecckFrequency);
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



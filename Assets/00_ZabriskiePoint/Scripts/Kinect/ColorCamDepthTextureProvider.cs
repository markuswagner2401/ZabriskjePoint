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


    // masking
    [SerializeField] int maskPercentage = 100;

    [SerializeField] float horizontalShift = 0;
    [SerializeField] float verticalShift = 0;

    int lastMaskPercentage;

    [SerializeField] MaskImage maskImage;

    [SerializeField] RenderTexture maskedImge;

    [SerializeField] RenderTexture maskedColorImage;

    [Tooltip("Turn off after setup/debugging")]
    [SerializeField] bool renderColorImage = true;

    // remapping depht shader

    [SerializeField] Vector2 depthShaderInMinMax;

    [SerializeField] float depthRangeStart = 0;
    [SerializeField] float depthRangeEnd = 1f;

    [SerializeField] float outMin;

    [SerializeField] float outMax;

    





    // DephsCheck

    public ComputeShader depthCheckShader;
    private int depthCheckKernel;

    public ComputeShader fillRectangelShader;

    private int fillRectangleKernel;


    private ComputeBuffer currentDepthBuffer;
    private ComputeBuffer prevDepthBuffer;
    private ComputeBuffer changeDataBuffer;
    private int[] currentDepthData;

    private int currentColorCamPixel;
    private int[] prevDepthData;
    private int[] changeData;

    [SerializeField] int pixelChangeThreshold = 100;

    [SerializeField] int changeCountBeforeTrigger = 2;
    [SerializeField] int changeCounter = 0;

    [SerializeField] int noChangeCounter = 0;





    [SerializeField] RenderTexture depthChangeDebug = null;
    RenderTexture writableDepthChangeDebug;

    [SerializeField] RenderTexture changingRectangleDebug = null;
    RenderTexture writableRectangleDebug;

    [SerializeField] int changingRectEdgeWidth = 10;

    [SerializeField] float changingAreaSmoothing = 0.1f;

    [SerializeField] int upperPercentile = 95;
    [SerializeField] int lowerPercentile = 5;

    int rightMost;
    int lastRightMost;
    int leftMost;
    int lastLeftMost;
    int upMost;
    int lastUpMost;
    int downMost;
    int lastDownMost;

    //

    // public struct SegmentInfo
    // {
    //     public int ClosestLength;
    //     public int PixelIndex;
    // }


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

    // Dictionary<int, int> lookupPixelSegment = new Dictionary<int, int>();

    // SegmentInfo[] closestPointsInSegments;

    // SegmentInfo[] lastClosestPointsInSegments;

    // [SerializeField] int heightSegments = 2;
    // [SerializeField] int widthSegments = 2;

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
        maskImage = GetComponent<MaskImage>();
        //Initialize();

        // if (materialPropertiesFader_2 == null)
        // {
        //     materialPropertiesFader_2 = GetComponent<MaterialPropertiesFader_2>();
        // }

    }

    void Update()
    {
        if (maskPercentage != lastMaskPercentage)
        {
            vfx?.Reinit();
        }

        lastMaskPercentage = maskPercentage;


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
                if (receiverMaterial != null && maskedColorImage != null)
                {
                    receiverMaterial.SetTexture(colorTextureRef, maskedColorImage);
                }

                //kinectManager.GetColorImageTex(sensorIndex)

                if (vfx != null && maskedColorImage != null)
                {
                    vfx.SetTexture(colorTextureRef, maskedColorImage);
                }

                colorTextureSet = true;
            }

        }

        if (nearestDistanceChanging)
        {
            changingBar = Mathf.Lerp(changingBar, 1f, Time.deltaTime * triggerDelayIn);

            if (materialPropertiesFader_2 != null && materialPropertiesFader_2.gameObject.activeInHierarchy)
            {
                materialPropertiesFader_2.UpdateFloat(2, 0.98f);
            }

            if (vFXParameterAnimator != null && vFXParameterAnimator.gameObject.activeInHierarchy)
            {
                vFXParameterAnimator.UpdateFloat(2, 0.98f);
            }

        }

        else
        {
            changingBar = Mathf.Lerp(changingBar, 0f, Time.deltaTime * triggerDelayOut);
            if (materialPropertiesFader_2 != null && materialPropertiesFader_2.gameObject.activeInHierarchy)
            {
                materialPropertiesFader_2.UpdateFloat(2, 0f);
            }

            if (vFXParameterAnimator != null && vFXParameterAnimator.gameObject.activeInHierarchy)
            {
                vFXParameterAnimator.UpdateFloat(2, 0f);
            }


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

        // if (changingPosition != Vector2.zero)
        // {
        //     if (materialPropertiesFader_2 != null && materialPropertiesFader_2.gameObject.activeInHierarchy)
        //     {
        //         materialPropertiesFader_2?.UpdateFloat(0, changingPosition.x);
        //         materialPropertiesFader_2?.UpdateFloat(1, changingPosition.y);
        //     }

        //     if (vFXParameterAnimator != null && vFXParameterAnimator.gameObject.activeInHierarchy)
        //     {
        //         vFXParameterAnimator?.UpdateFloat(0, changingPosition.x);
        //         vFXParameterAnimator?.UpdateFloat(1, changingPosition.y);
        //     }

        // }



    }

    // Initializing

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
            //depthImageMaterial = new Material(Shader.Find("Kinect/DepthHistImageShaderSWRemap"));
            depthImageMaterial = new Material(Shader.Find("Kinect/DepthDirectRemap"));

            depthImageMaterial.SetFloat("_DepthRangeStart", depthRangeStart);
            depthImageMaterial.SetFloat("_DepthRangeEnd", depthRangeEnd);
            depthImageMaterial.SetFloat("_OutMin", outMin);
            depthImageMaterial.SetFloat("_OutMax", outMax);

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
                //receiverMaterial.SetTexture(depthTextureRef, depthImageTexture);
                receiverMaterial.SetTexture(depthTextureRef, maskedImge);
                print("Set Texture at receiver material");
            }

            if (vfx != null)
            {
                //vfx.SetTexture(depthTextureRef, depthImageTexture);
                vfx.SetTexture(depthTextureRef, maskedImge);
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

        writableRectangleDebug = new RenderTexture(depthImageTexture.width, depthImageTexture.height, 24);
        writableRectangleDebug.enableRandomWrite = true;
        writableRectangleDebug.Create();

        // Copying data
        for (int i = 0; i < sensorData.colorCamDepthImage.Length; i++)
        {
            currentDepthData[i] = sensorData.colorCamDepthImage[i];
        }

        System.Array.Copy(currentDepthData, prevDepthData, currentDepthData.Length);

        // Fill Changing Rectangle

        fillRectangleKernel = fillRectangelShader.FindKernel("FillRectangle");




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

        // CreatePixelSegmentLookup(depthImageTexture.width, depthImageTexture.height);

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

        currentDepthBuffer?.Dispose();
        prevDepthBuffer?.Dispose();
        changeDataBuffer?.Dispose();

        writableDepthChangeDebug.Release();

        writableRectangleDebug.Release();



        colorTextureSet = false;

        //lookupPixelSegment.Clear();

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

        currentDepthBuffer?.Release();
        prevDepthBuffer?.Release();
        changeDataBuffer?.Release();
    }

    // Debugging

    public void DebugOnDephtsChangingEnd()
    {
        onDepthChangingEnd.Invoke();
    }

    public void DebugOnDepthChaningStart()
    {
        onDepthChangingStart.Invoke();
    }

    // Depth Check

    IEnumerator DepthsCheckRoutine()
    {

        bool kickstart = true;

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
            depthCheckShader.SetInt("depthImageHeight", depthImageTexture.height);
            depthCheckShader.SetInt("maxDepthDistance", DepthSensorBase.MAX_DEPTH_DISTANCE_MM);
            depthCheckShader.SetInt("distanceChangeThreshold", distChangeThreshold);
            depthCheckShader.SetInt("maskPercentage", maskPercentage);

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
            
            int sumPosX = 0;
            int sumPosY = 0;
            int changingPixels = 0;

            // changinArea
            // rightMost = depthImageTexture.width;
            // leftMost = -1;
            // upMost = depthImageTexture.height;
            // downMost = -1;

            List<int> posXList = new List<int>();
            List<int> posYList = new List<int>();

            //nearestDistanceChanging = false;
            bool changingThisFrame = false;
            

            for (int i = 0; i < changeData.Length; i++)
            {
                
                int limDepth = (changeData[i] <= DepthSensorBase.MAX_DEPTH_DISTANCE_MM) ? changeData[i] : 0;
                if (limDepth == 1)
                {

                    changingPixels += 1;
                    if (changingPixels > pixelChangeThreshold)
                    {
                        changingThisFrame = true;
                        
                        //nearestDistanceChanging = true;
                    }

                    int posX = i % depthImageTexture.width;
                    int posY = i / depthImageTexture.width;

                    posXList.Add(posX);
                    posYList.Add(posY);

                    // Check for outermost changing pixels
                    // rightMost = Mathf.Min(rightMost, posX);
                    // leftMost = Mathf.Max(leftMost, posX);
                    // upMost = Mathf.Min(upMost, posY);
                    // downMost = Mathf.Max(downMost, posY);


                    // caluclating of avarage changing position
                    sumPosX += posX;
                    sumPosY += posY;

                    prevDepthData[i] = currentDepthData[i]; // only change prevDeppthBuffer for changing pixels.
                }
            }

            // check this area

            if(changingThisFrame)
            {
                changeCounter += 1;
                noChangeCounter = 0;

                if(changeCounter >= changeCountBeforeTrigger)
                {
                    nearestDistanceChanging = true;
                }
            }

            else
            {
                noChangeCounter += 1;
                changeCounter = 0;
                if(noChangeCounter >=changeCountBeforeTrigger)
                {
                    nearestDistanceChanging = false;
                }
            }

            

            if (nearestDistanceChanging)
            {
                // changing rectangle



                posXList.Sort();
                posYList.Sort();

                int lowerPercentileIndex = posXList.Count * lowerPercentile / 100; // lowerPercentile is an int between 0 and 100
                int upperPercentileIndex = posXList.Count * upperPercentile / 100; // upperPercentile is an int between 0 and 100



                leftMost = posXList[upperPercentileIndex];
                rightMost = posXList[lowerPercentileIndex];
                upMost = posYList[lowerPercentileIndex];
                downMost = posYList[upperPercentileIndex];

                rightMost = (int)Mathf.Floor(Mathf.Lerp(lastRightMost, rightMost, changingAreaSmoothing));
                leftMost = (int)Mathf.Floor(Mathf.Lerp(lastLeftMost, leftMost, changingAreaSmoothing));
                upMost = (int)Mathf.Floor(Mathf.Lerp(lastUpMost, upMost, changingAreaSmoothing));
                downMost = (int)Mathf.Floor(Mathf.Lerp(lastDownMost, downMost, changingAreaSmoothing));

                lastRightMost = rightMost;
                lastLeftMost = leftMost;
                lastUpMost = upMost;
                lastDownMost = downMost;




                fillRectangelShader.SetTexture(fillRectangleKernel, "outputTexture", writableRectangleDebug);
                fillRectangelShader.SetInt("left", leftMost);
                fillRectangelShader.SetInt("right", rightMost);
                fillRectangelShader.SetInt("up", upMost);
                fillRectangelShader.SetInt("down", downMost);
                fillRectangelShader.SetInt("edgeWidth", changingRectEdgeWidth);  // Set edge width
                fillRectangelShader.SetInts("textureSize", new int[2] { writableRectangleDebug.width, writableRectangleDebug.height });

                fillRectangelShader.Dispatch(fillRectangleKernel, writableRectangleDebug.width / 8, writableRectangleDebug.height / 8, 1);

                if (writableRectangleDebug.width == changingRectangleDebug.width && writableRectangleDebug.height == changingRectangleDebug.height)
                {
                    Graphics.Blit(writableRectangleDebug, changingRectangleDebug);

                }
                else
                {
                    Debug.LogError("Texture Resolution Mismach: writableRectangleDebug, changingRectangleDebug");
                }




                

                kickstart = false; // after the a changing was detected the system is running and we only set prevDepthData for changing pixels

            }

            else
            {
                // changingPosition = Vector3.zero;
                // 
            }

            if (kickstart)
            {
                System.Array.Copy(currentDepthData, prevDepthData, currentDepthData.Length);
            }


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

            //print("Min and Max Distance set for depthHistShader: " + depthMinDistance + " " + depthMaxDistance);

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

            depthImageMaterial.SetFloat("_DepthRangeStart", depthRangeStart);
            depthImageMaterial.SetFloat("_DepthRangeEnd", depthRangeEnd);
            depthImageMaterial.SetFloat("_OutMin", outMin);
            depthImageMaterial.SetFloat("_OutMax", outMax);




            if (nearestDistanceChanging)
            {
                Graphics.Blit(null, depthImageTexture, depthImageMaterial);

                Graphics.Blit(maskImage.GetMaskedImage(depthImageTexture, maskedImge, maskPercentage, verticalShift, horizontalShift), maskedImge);

                if(renderColorImage)
                {
                    Graphics.Blit(maskImage.GetMaskedImage(kinectManager.GetColorImageTex(sensorIndex), maskedColorImage, maskPercentage, verticalShift, horizontalShift), maskedColorImage);
                }
            }


        }


    }

}



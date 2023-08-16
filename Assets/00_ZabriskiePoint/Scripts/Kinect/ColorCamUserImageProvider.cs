using UnityEngine;
using System.Collections;
using com.rfilkov.kinect;
using System;
using UnityEngine.VFX;

namespace com.rfilkov.components
{
    /// <summary>
    /// BackgroundColorCamUserImage is component that displays the color camera aligned user-body image on RawImage texture, usually the scene background.
    /// </summary>
    public class ColorCamUserImageProvider : MonoBehaviour
    {
        [SerializeField] Material receiverMaterial;

        [SerializeField] public VisualEffect vfx = null;

        public String colorTextureRef = "_ColorTexture";

        public String bodyImageRef = "_UserImage";

        public String userMaskRef = "_UserMask";

        public String defaultTextureRef = "_DefaultTexture";

        public RenderTexture defaultTexture;

        public RenderTexture userMask;

        public RenderTexture colorImage;

        public RenderTexture bodyImage;

        public ExtendMask extendMask = null;

        public int offsetMask = 5;

        bool colorTextureSet = false;

        ////
        [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
        public int sensorIndex = 0;

        [Tooltip("Index of the player, tracked by this component. -1 means all players, 0 - the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
        public int playerIndex = -1;

        // [Tooltip("RawImage used to display the color camera feed.")]
        // public UnityEngine.UI.RawImage backgroundImage;

        // [Tooltip("Camera used to display the background image. Set it, if you'd like to allow background image to resize, to match the color image's aspect ratio.")]
        // public Camera backgroundCamera;


        // last camera rect width & height
        private float lastCamRectW = 0;
        private float lastCamRectH = 0;

        // reference to the kinectManager
        private KinectManager kinectManager = null;
        private KinectInterop.SensorData sensorData = null;
        private Vector2 initialAnchorPos = Vector2.zero;

        // color-camera aligned frames
        private ulong lastColorCamDepthFrameTime = 0;
        private ulong lastColorCamBodyIndexFrameTime = 0;

        // color-camera aligned texture and buffers
        private RenderTexture bodyImageTexture = null;

      
        private Material bodyImageMaterial = null;

        private ComputeBuffer bodyIndexBuffer = null;
        private ComputeBuffer depthImageBuffer = null;
        private ComputeBuffer bodyHistBuffer = null;

        // body image hist data
        protected int[] depthBodyBufferData = null;
        protected int[] equalBodyBufferData = null;
        protected int bodyHistTotalPoints = 0;


        void Start()
        {

            Initialize();
        }

        public void Initialize()
        {
            if (extendMask == null)
            {
                extendMask = GetComponent<ExtendMask>();

            }
            kinectManager = KinectManager.Instance;
            sensorData = kinectManager != null ? kinectManager.GetSensorData(sensorIndex) : null;

            if (sensorData != null)
            {

                // enable the color camera aligned depth & body-index frames 
                sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, true);
                sensorData.sensorInterface.EnableColorCameraBodyIndexFrame(sensorData, true);

                // create the user texture and needed buffers
                bodyImageTexture = KinectInterop.CreateRenderTexture(bodyImageTexture, sensorData.colorImageWidth, sensorData.colorImageHeight);
                bodyImageMaterial = new Material(Shader.Find("Kinect/UserHistImageShader_bw"));

                bodyHistBuffer = KinectInterop.CreateComputeBuffer(bodyHistBuffer, DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1, sizeof(int));

                depthBodyBufferData = new int[DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1];
                equalBodyBufferData = new int[DepthSensorBase.MAX_DEPTH_DISTANCE_MM + 1];
            }

            if (bodyImageTexture != null)
            {
                if (receiverMaterial != null)
                {
                    receiverMaterial.SetTexture(bodyImageRef, bodyImageTexture);
                    print("Set Texture at receiver material");
                }

                // if (vfx != null)
                // {
                //     print("set body image at vfx");
                //     vfx.SetTexture(bodyImageRef, bodyImageTexture);
                // }
            }

        }

        public void Deinitialize()
        {
            StopAllCoroutines();

            if (bodyImageTexture)
            {
                bodyImageTexture.Release();
                bodyImageTexture = null;
            }

            if (bodyIndexBuffer != null)
            {
                bodyIndexBuffer.Dispose();
                bodyIndexBuffer = null;
            }

            if (depthImageBuffer != null)
            {
                depthImageBuffer.Dispose();
                depthImageBuffer = null;
            }

            if (bodyHistBuffer != null)
            {
                bodyHistBuffer.Dispose();
                bodyHistBuffer = null;
            }

            if (sensorData != null)
            {
                // disable the color camera aligned depth & body-index frames 
                sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, false);
                sensorData.sensorInterface.EnableColorCameraBodyIndexFrame(sensorData, false);
            }

        }


        void OnDestroy()
        {
            if (bodyImageTexture)
            {
                bodyImageTexture.Release();
                bodyImageTexture = null;
            }

            if (bodyIndexBuffer != null)
            {
                bodyIndexBuffer.Dispose();
                bodyIndexBuffer = null;
            }

            if (depthImageBuffer != null)
            {
                depthImageBuffer.Dispose();
                depthImageBuffer = null;
            }

            if (bodyHistBuffer != null)
            {
                bodyHistBuffer.Dispose();
                bodyHistBuffer = null;
            }

            if (sensorData != null)
            {
                // disable the color camera aligned depth & body-index frames 
                sensorData.sensorInterface.EnableColorCameraDepthFrame(sensorData, false);
                sensorData.sensorInterface.EnableColorCameraBodyIndexFrame(sensorData, false);
            }
        }

        public void CaptureDefaultTexture()
        {
            print("capture default texture");

            if (kinectManager.GetColorImageTex(sensorIndex).width == defaultTexture.width && kinectManager.GetColorImageTex(sensorIndex).height == defaultTexture.height)
            {
                Graphics.Blit(kinectManager.GetColorImageTex(sensorIndex), defaultTexture);
            }
            else
            {
                Debug.LogError("Texture Resolution Mismatch: ColorImageTexture, defaultTexture");
            }

            receiverMaterial.SetTexture(defaultTextureRef, defaultTexture);

        }

        void Update()
        {
            if (kinectManager && kinectManager.IsInitialized() && sensorData != null)
            {
                // if(Input.GetKeyDown(KeyCode.P))
                // {
                //     StartCoroutine(GetComponent<TextureSaver>().WaitAndSaveRenderTextureToDisk(bodyImage, "BodyImage", 5f));
                // }


                // check for new color camera aligned frames
                UpdateTextureWithNewFrame();




                //////

                if (!colorTextureSet)
                {
                    if (receiverMaterial != null)
                    {
                        receiverMaterial.SetTexture(colorTextureRef, kinectManager.GetColorImageTex(sensorIndex));
                    }

                    // if (vfx != null)
                    // {
                    //     vfx.SetTexture(colorTextureRef, colorImage);
                    //     print(vfx.GetTexture(colorTextureRef).name);
                    // }

                    colorTextureSet = true;
                }


            }

        }


        // checks for new color-camera aligned frames, and composes an updated body-index texture, if needed
        private void UpdateTextureWithNewFrame()
        {
            if (sensorData == null || sensorData.sensorInterface == null || sensorData.colorCamBodyIndexImage == null || sensorData.colorCamDepthImage == null)
                return;
            if (sensorData.colorImageWidth == 0 || sensorData.colorImageHeight == 0 || sensorData.lastColorCamDepthFrameTime == 0 || sensorData.lastColorCamBodyIndexFrameTime == 0)
                return;

            // get body index frame
            if (lastColorCamDepthFrameTime != sensorData.lastColorCamDepthFrameTime || lastColorCamBodyIndexFrameTime != sensorData.lastColorCamBodyIndexFrameTime)
            {
                lastColorCamDepthFrameTime = sensorData.lastColorCamDepthFrameTime;
                lastColorCamBodyIndexFrameTime = sensorData.lastColorCamBodyIndexFrameTime;

                if (bodyImageTexture == null || bodyImageTexture.width != sensorData.colorImageWidth || bodyImageTexture.height != sensorData.colorImageHeight)
                {
                    bodyImageTexture = KinectInterop.CreateRenderTexture(bodyImageTexture, sensorData.colorImageWidth, sensorData.colorImageHeight);
                    // if (vfx != null)
                    // {
                    //     print("set body image at vfx");
                    //     vfx.SetTexture(bodyImageRef, bodyImageTexture);
                    // }
                }

                Array.Clear(depthBodyBufferData, 0, depthBodyBufferData.Length);
                Array.Clear(equalBodyBufferData, 0, equalBodyBufferData.Length);
                bodyHistTotalPoints = 0;

                // get configured min & max distances 
                float minDistance = ((DepthSensorBase)sensorData.sensorInterface).minDepthDistance;
                float maxDistance = ((DepthSensorBase)sensorData.sensorInterface).maxDepthDistance;

                int depthMinDistance = (int)(minDistance * 1000f);
                int depthMaxDistance = (int)(maxDistance * 1000f);

                int frameLen = sensorData.colorCamDepthImage.Length;
                for (int i = 0; i < frameLen; i++)
                {
                    int depth = sensorData.colorCamDepthImage[i];
                    int limDepth = (depth >= depthMinDistance && depth <= depthMaxDistance) ? depth : 0;

                    if (/**rawBodyIndexImage[i] != 255 &&*/ limDepth > 0)
                    {
                        depthBodyBufferData[limDepth]++;
                        bodyHistTotalPoints++;
                    }
                }

                if (bodyHistTotalPoints > 0)
                {
                    equalBodyBufferData[0] = depthBodyBufferData[0];
                    for (int i = 1; i < depthBodyBufferData.Length; i++)
                    {
                        equalBodyBufferData[i] = equalBodyBufferData[i - 1] + depthBodyBufferData[i];
                    }
                }

                int bodyIndexBufferLength = sensorData.colorCamBodyIndexImage.Length >> 2;
                if (bodyIndexBuffer == null || bodyIndexBuffer.count != bodyIndexBufferLength)
                {
                    bodyIndexBuffer = KinectInterop.CreateComputeBuffer(bodyIndexBuffer, bodyIndexBufferLength, sizeof(uint));
                }

                KinectInterop.SetComputeBufferData(bodyIndexBuffer, sensorData.colorCamBodyIndexImage, bodyIndexBufferLength, sizeof(uint));

                int depthBufferLength = sensorData.colorCamDepthImage.Length >> 1;
                if (depthImageBuffer == null || depthImageBuffer.count != depthBufferLength)
                {
                    depthImageBuffer = KinectInterop.CreateComputeBuffer(depthImageBuffer, depthBufferLength, sizeof(uint));
                }

                KinectInterop.SetComputeBufferData(depthImageBuffer, sensorData.colorCamDepthImage, depthBufferLength, sizeof(uint));

                if (bodyHistBuffer != null)
                {
                    KinectInterop.SetComputeBufferData(bodyHistBuffer, equalBodyBufferData, equalBodyBufferData.Length, sizeof(int));
                }

                float minDist = minDistance;  // kinectManager.minUserDistance != 0f ? kinectManager.minUserDistance : minDistance;
                float maxDist = maxDistance;  // kinectManager.maxUserDistance != 0f ? kinectManager.maxUserDistance : maxDistance;

                bodyImageMaterial.SetInt("_TexResX", sensorData.colorImageWidth);
                bodyImageMaterial.SetInt("_TexResY", sensorData.colorImageHeight);
                bodyImageMaterial.SetInt("_MinDepth", (int)(minDist * 1000f));
                bodyImageMaterial.SetInt("_MaxDepth", (int)(maxDist * 1000f));

                bodyImageMaterial.SetBuffer("_BodyIndexMap", bodyIndexBuffer);
                bodyImageMaterial.SetBuffer("_DepthMap", depthImageBuffer);
                bodyImageMaterial.SetBuffer("_HistMap", bodyHistBuffer);
                bodyImageMaterial.SetInt("_TotalPoints", bodyHistTotalPoints);

                Color[] bodyIndexColors = kinectManager.GetBodyIndexColors();
                if (playerIndex >= 0)
                {
                    ulong userId = kinectManager.GetUserIdByIndex(playerIndex);
                    int bodyIndex = kinectManager.GetBodyIndexByUserId(userId);

                    int numBodyIndices = bodyIndexColors.Length;
                    Color clrNone = new Color(0f, 0f, 0f, 0f);

                    for (int i = 0; i < numBodyIndices; i++)
                    {
                        if (i != bodyIndex)
                            bodyIndexColors[i] = clrNone;
                    }
                }

                bodyImageMaterial.SetColorArray("_BodyIndexColors", bodyIndexColors);

                Graphics.Blit(null, bodyImageTexture, bodyImageMaterial);

                Graphics.Blit(bodyImageTexture, bodyImage); // workaround for vfx

                Graphics.Blit(kinectManager.GetColorImageTex(0), colorImage);// workaround for vfx

                // extended Mask
                if (extendMask != null)
                {
                    Graphics.Blit(extendMask.GetExtendedMask(bodyImageTexture, offsetMask), userMask);
                }

            }
        }



    }
}


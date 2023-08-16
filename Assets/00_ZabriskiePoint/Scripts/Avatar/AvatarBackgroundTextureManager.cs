using com.rfilkov.components;
using com.rfilkov.kinect;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AvatarBackgroundTextureManager : MonoBehaviour
{
    [SerializeField] ColorCamUserImageProvider colorCamUserImageProvider = null;
    [SerializeField] bool startAutomaticly;

    [SerializeField] float captureFreqMin = 1f;
    [SerializeField] float captureFreqMax = 10f;

    bool fistCaptureDone = false;

    

    
    void Start()
    {
        if(colorCamUserImageProvider == null) 
        { 
            colorCamUserImageProvider = GetComponent<ColorCamUserImageProvider>();
        }
        if (colorCamUserImageProvider == null) return;


        if (startAutomaticly)
        {
            StartCaptureRoutine();
        }
        

    }

    private void Update()
    {
        if(fistCaptureDone) return;
        if(KinectManager.Instance.IsInitialized())
        {
            fistCaptureDone = true;
            print("Capture first image");
            colorCamUserImageProvider?.CaptureDefaultTexture();


        }
            

    }

    public void StartCaptureRoutine()
    {
        StartCoroutine(CaptureRoutine());
    }

    

    IEnumerator CaptureRoutine() 
    {
        while(true)
        {
            yield return CaptureNextImage();
        }
        
    }

    IEnumerator CaptureNextImage()
    {
        float timer = 0;
        float freq = Random.Range(captureFreqMin, captureFreqMax);  
        while (timer < freq)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        colorCamUserImageProvider.CaptureDefaultTexture();

        yield break;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRTUpdater : MonoBehaviour
{
    public CustomRenderTexture customRenderTexture;

    public int updateCount = 4;

    void Start()
    {
        customRenderTexture.Initialize();
    }


    void FixedUpdate()
    {
        customRenderTexture.Update(updateCount);
    }
}

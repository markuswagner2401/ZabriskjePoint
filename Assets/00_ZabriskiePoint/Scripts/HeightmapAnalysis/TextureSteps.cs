using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureSteps : MonoBehaviour
{
    public ComputeShader stepsComputeShader;
    
    

    public RenderTexture TextureToSteps(RenderTexture inputTexture, int steps)
    {
        RenderTexture outputTexture = new RenderTexture(inputTexture.width, inputTexture.height,  0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            enableRandomWrite = true
        };

        outputTexture.Create();

        Graphics.Blit(inputTexture, outputTexture);

        // Calculate the number of thread groups
        int threadGroupsX = Mathf.CeilToInt(outputTexture.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(outputTexture.height / 8f);
        int threadGroupsZ = 1;

        stepsComputeShader.SetTexture(0, "InputTexture", inputTexture);
        stepsComputeShader.SetInt("Steps", steps);
        stepsComputeShader.SetTexture(0, "Result", outputTexture);

        stepsComputeShader.Dispatch(0, threadGroupsX, threadGroupsY, threadGroupsZ);

        Graphics.Blit(outputTexture, inputTexture);

        outputTexture.Release();

        return inputTexture;
    }
}

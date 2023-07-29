using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaskImage : MonoBehaviour
{
    public ComputeShader computeShader;
  

    public RenderTexture GetMaskedImage(RenderTexture inputTexture, RenderTexture targetTexture, int maskPercentage)
    {
        // Calculate the dimensions of the masked image
        int maskWidth = inputTexture.width * maskPercentage / 100;
        int maskHeight = inputTexture.height * maskPercentage / 100;

        // Create the output texture
        RenderTexture outputTexture = new RenderTexture(maskWidth, maskHeight, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        // Find the kernel in the shader
        int maskKernel = computeShader.FindKernel("MaskImage");

        // Set parameters for the shader
        computeShader.SetTexture(maskKernel, "inputTexture", inputTexture);
        computeShader.SetTexture(maskKernel, "outputTexture", outputTexture);
        computeShader.SetInt("maskPercentage", maskPercentage);
        computeShader.SetInts("inputResolution", new int[] { inputTexture.width, inputTexture.height });
        computeShader.SetInts("outputResolution", new int[] { outputTexture.width, outputTexture.height });

        // Dispatch the shader
        computeShader.Dispatch(maskKernel, outputTexture.width / 8, outputTexture.height / 8, 1);

        // Return the output texture



        Graphics.Blit(outputTexture, targetTexture);

        outputTexture.Release();

        return targetTexture;
    }

   
}

using UnityEngine;

public class ExtendMask : MonoBehaviour
{
    // Set this in the Inspector
    public ComputeShader ExtendMaskComputeShader;

    public RenderTexture GetExtendedMask(RenderTexture inputTexture, int offset)
    {
        // Get the kernel index
        int kernelHandle = ExtendMaskComputeShader.FindKernel("ExtendMask");

        // Set the input mask
        ExtendMaskComputeShader.SetTexture(kernelHandle, "_InputMask", inputTexture);

        // Create the output texture
        RenderTexture output = new RenderTexture(inputTexture.width, inputTexture.height, 24);
        output.enableRandomWrite = true;
        output.Create();

        // Set the output texture
        ExtendMaskComputeShader.SetTexture(kernelHandle, "_Output", output);

        // Set the offset
        ExtendMaskComputeShader.SetInt("_OffsetMask", offset);

        // Define the number of thread groups
        int threadGroupsX = Mathf.CeilToInt(inputTexture.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(inputTexture.height / 8.0f);

        // Dispatch the compute shader (start it)
        ExtendMaskComputeShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

        return output;
    }
}
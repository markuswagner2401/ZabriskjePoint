using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlurTexture : MonoBehaviour
{

    // compute Shaders
    public ComputeShader BlurShader;
    public ComputeShader AuxiliaryShader;

    //Compute buffers

    private ComputeBuffer buffer1; //used by various kernels
    private ComputeBuffer buffer2; //used by various kernels
    private List<ComputeBuffer> buffers = new List<ComputeBuffer>();

    private RenderTexture OutputTexture;
    private RenderTexture writeableRT;


    int width;
    int height;
    int length;


   
    private void OnDestroy()
    {
        foreach (var buffer in buffers)
        {
            if (buffer != null)
            {
                buffer.Release();
            }
        }
    }



    public RenderTexture Blur(RenderTexture inputTexture)
    {
        width = inputTexture.width;
        height = inputTexture.height;
        length = width * height;

        //set up Render textures

        writeableRT = new RenderTexture(width, height, 24);
        writeableRT.enableRandomWrite = true;
        writeableRT.Create();
        Graphics.Blit(inputTexture, writeableRT);

        OutputTexture = new RenderTexture(width, height, 24);
        OutputTexture.enableRandomWrite = true;
        OutputTexture.Create();


        BlurShader.SetInt("width", width);
        BlurShader.SetInt("height", height);

        AuxiliaryShader.SetInt("width", width);
        AuxiliaryShader.SetInt("height", height);

        //create compute buffers
        buffer1 = new ComputeBuffer(length, sizeof(float));
        buffer2 = new ComputeBuffer(length, sizeof(float));

        //add compute buffers to the list of compute buffers, used in onDestroy() for .Release() method
        buffers = new List<ComputeBuffer>() {
            buffer1,
            buffer2,
        };

        #region Zero Compute Buffers

        //Clear all buffers, required for good results, some buffers do not have to be reseted from here so further micro-optimization possible
        int auxiliaryHandle = AuxiliaryShader.FindKernel("ClearBuffer");



        //Zero each buffer, this is requirement to reset state after each FixedUpdate.
        //It is not necessary for algorithms that are executed only once.
        foreach (var buffer in buffers)
        {
            AuxiliaryShader.SetBuffer(auxiliaryHandle, "buffer", buffer);
            int count = buffer.count * (buffer.stride / 4); // because some buffer have stride > 4, e.g. Lines
            AuxiliaryShader.Dispatch(auxiliaryHandle, Mathf.CeilToInt(count / 64f), Mathf.CeilToInt(1 / 1f), 1);
        }

        #endregion Zero Compute Buffers


        //--------------------------------------------------------------------------------------------//

        #region Convert Texture to Buffer

        //Auxiliary shaders contains miscellaneous methods e.g. converstion from/to buffer, drawing lines etc.
        //Turn the texture into a compute buffer, this buffer is then utilized by other algorithms
        auxiliaryHandle = AuxiliaryShader.FindKernel("TextureToBuffer");
        AuxiliaryShader.SetTexture(auxiliaryHandle, "Texture", writeableRT);
        AuxiliaryShader.SetBuffer(auxiliaryHandle, "buffer", buffer2);
        AuxiliaryShader.Dispatch(auxiliaryHandle, Mathf.CeilToInt(width / 64f), height, 1);

        #endregion Convert Texture to Buffer

        #region Blur the texture

        //This shader peforms blurring, it improves Canny Edge Detection output.
        //Other blur methods can be used as well.
        //This kernel is divided into two passes for better performance.

        int blurHandle = BlurShader.FindKernel("GaussianBlur");

        //perform X-pass
        BlurShader.SetBuffer(blurHandle, "inputBuffer", buffer2);
        BlurShader.SetBuffer(blurHandle, "outputBuffer", buffer1);
        BlurShader.SetBool("Horizontal", true);
        BlurShader.Dispatch(blurHandle, Mathf.CeilToInt(width / 64f), height, 1);

        //perform Y-pass
        BlurShader.SetBuffer(blurHandle, "inputBuffer", buffer1);
        BlurShader.SetBuffer(blurHandle, "outputBuffer", buffer2);
        BlurShader.SetBool("Horizontal", false);
        BlurShader.Dispatch(blurHandle, Mathf.CeilToInt(width / 64f), height, 1);

        //ConvertToTexture(buffer2, OutputTexture);

        #endregion Blur the texture

        int kernelHandle = AuxiliaryShader.FindKernel("BufferToTexture");
        AuxiliaryShader.SetBuffer(kernelHandle, "buffer", buffer2);
        AuxiliaryShader.SetTexture(kernelHandle, "Texture", OutputTexture);
        AuxiliaryShader.Dispatch(kernelHandle, Mathf.CeilToInt(width / 64f), height, 1);

        Graphics.Blit(OutputTexture, inputTexture);

        writeableRT.Release();
        OutputTexture.Release();

        return inputTexture;

    }

    RenderTexture CreateWritableRendertexture(Texture texture)
    {
        width = texture.width;
        height = texture.height;
        length = width * height;

        writeableRT = new RenderTexture(width, height, 24);
        writeableRT.enableRandomWrite = true;
        writeableRT.Create();

        Graphics.Blit(texture, writeableRT);

        return writeableRT;
    }



}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RippleShaderController : MonoBehaviour
{
    struct PositionData
    {
        public Vector2 position;
        public float size;

        public float strength;
    }
    public ComputeShader rippleComputeShader;

    public float lineSize;
    public float minSize;
    public float maxSize;
    public float minStrength;
    public float maxStrength;

    public float tilingX = 1f;

    public AnimationCurve sizeCurve;

    public AnimationCurve strengthCurve;

    public float duration;


    private RenderTexture outputTexture;

    public RenderTexture ripplesRT;

    private int kernelHandle;
    private List<ComputeBuffer> buffers = new List<ComputeBuffer>();



    private Vector2[] positionsHills = new Vector2[0];

    private Vector2[] positionsTroughs = new Vector2[0];
    private float[] agesHills = new float[0];

    private float[] agesTroughs = new float[0];

    Texture2D blackTexture;



    private void Start()
    {
        outputTexture = new RenderTexture(ripplesRT.width, ripplesRT.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        // black texture

        blackTexture = new Texture2D(1, 1);

        blackTexture.SetPixel(0, 0, Color.black);
        blackTexture.Apply();

        Graphics.Blit(blackTexture, ripplesRT);





        kernelHandle = rippleComputeShader.FindKernel("Ripple");

        rippleComputeShader.SetInt("outputWidth", outputTexture.width);
        rippleComputeShader.SetInt("outputHeight", outputTexture.height);

        tilingX = (float)outputTexture.width / (float)outputTexture.height;

        GetComponent<Renderer>().material.mainTexture = outputTexture;
    }

    private void OnDestroy()
    {

        foreach (var item in buffers)
        {
            item?.Release();
        }

        outputTexture?.Release();
    }

    public void CreateRipples(Vector2[] newPositionsHills, Vector2[] newPositionsTroughs)
    {
        if (newPositionsHills == null || newPositionsTroughs == null)
        {
            Debug.LogError("New positions cannot be null");
            return;
        }


        rippleComputeShader.SetTexture(kernelHandle, "outputTexture", outputTexture);

        positionsHills = new Vector2[newPositionsHills.Length];
        for (int i = 0; i < newPositionsHills.Length; i++)
        {
            // Convert world coordinates to UV coordinates
            positionsHills[i] = new Vector2(newPositionsHills[i].x / outputTexture.width, newPositionsHills[i].y / outputTexture.height);
        }

        positionsTroughs = new Vector2[newPositionsTroughs.Length];
        for (int i = 0; i < newPositionsTroughs.Length; i++)
        {
            // Convert world coordinates to UV coordinates
            positionsTroughs[i] = new Vector2(newPositionsTroughs[i].x / outputTexture.width, newPositionsTroughs[i].y / outputTexture.height);
        }


        agesHills = new float[positionsHills.Length];

        agesTroughs = new float[positionsTroughs.Length];

        ComputeBuffer positionsHillsBuffer = null;

        print("create ripples for " + positionsHills.Length);

        if (positionsHills.Length > 0)
        {
            positionsHillsBuffer = new ComputeBuffer(positionsHills.Length, sizeof(float) * 4);
            buffers.Add(positionsHillsBuffer);
            rippleComputeShader.SetBuffer(kernelHandle, "positionsHills", positionsHillsBuffer);
        }





        ComputeBuffer positionsTroughsBuffer = null;

        if (positionsTroughs.Length > 0)
        {
            positionsTroughsBuffer = new ComputeBuffer(positionsTroughs.Length, sizeof(float) * 4);
            buffers.Add(positionsTroughsBuffer);
            rippleComputeShader.SetBuffer(kernelHandle, "positionsTroughs", positionsTroughsBuffer);

        }




        StartCoroutine(AnimateRipples(duration, positionsHillsBuffer, positionsTroughsBuffer));
    }

    private IEnumerator AnimateRipples(float duration, ComputeBuffer positionsHillsBuffer, ComputeBuffer positionsTroughsBuffer)
    {
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            //float newAge = time / duration;

            float newSize = Mathf.Lerp(0, 1f, sizeCurve.Evaluate(time / duration));
            float newStrength = Mathf.Lerp(0, 1f, strengthCurve.Evaluate(time / duration));

            if (this.positionsHills.Length > 0)
            {
                PositionData[] positionDataHills = new PositionData[this.positionsHills.Length];
                for (int i = 0; i < this.positionsHills.Length; i++)
                {
                    positionDataHills[i] = new PositionData
                    {
                        position = this.positionsHills[i],
                        //age = agesHills[i]
                        size = newSize,

                        strength = newStrength
                    };
                }
                positionsHillsBuffer.SetData(positionDataHills);
            }

            if (this.positionsTroughs.Length > 0)
            {
                PositionData[] positionDataTroughs = new PositionData[this.positionsTroughs.Length];
                for (int i = 0; i < this.positionsTroughs.Length; i++)
                {
                    positionDataTroughs[i] = new PositionData
                    {
                        position = this.positionsTroughs[i],
                        //age = agesTroughs[i]
                        size = newSize,

                        strength = newStrength
                    };
                }
                positionsTroughsBuffer.SetData(positionDataTroughs);
            }





            rippleComputeShader.SetInt("positionHillCount", this.positionsHills.Length);
            rippleComputeShader.SetInt("positionTroughCount", this.positionsTroughs.Length);

            rippleComputeShader.SetFloat("lineSize", lineSize);
            rippleComputeShader.SetFloat("minSize", minSize);
            rippleComputeShader.SetFloat("maxSize", maxSize);
            rippleComputeShader.SetFloat("minStrength", minStrength);
            rippleComputeShader.SetFloat("maxStrength", maxStrength);
            rippleComputeShader.SetFloat("maxValue", 1.0f); // Set maxValue
            rippleComputeShader.SetFloat("tilingX", tilingX);

            rippleComputeShader.Dispatch(kernelHandle, outputTexture.width / 8, outputTexture.height / 8, 1);

            if (ripplesRT.width == outputTexture.width && ripplesRT.height == outputTexture.height)
            {
                Graphics.Blit(outputTexture, ripplesRT);
            }

            else
            {
                Debug.LogError("resolution mismatch: outputTexture, rippleRT");
            }







            yield return null;
        }


        Graphics.Blit(blackTexture, ripplesRT);

        if (positionsHillsBuffer != null)
        {
            buffers.Remove(positionsHillsBuffer);
            positionsHillsBuffer.Release();
        }
        if (positionsTroughsBuffer != null)
        {
            buffers.Remove(positionsTroughsBuffer);
            positionsTroughsBuffer.Release();
        }
    }
}

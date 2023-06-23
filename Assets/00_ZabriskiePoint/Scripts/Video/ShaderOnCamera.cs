using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShaderOnCamera : MonoBehaviour
{
    public Material EffectMaterial;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        print("OnRenderImage");
        if (EffectMaterial != null)
            Graphics.Blit(source, destination, EffectMaterial);
        else
            Graphics.Blit(source, destination);
    }
}
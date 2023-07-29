using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXGradientBlender : MonoBehaviour
{
    [SerializeField] VisualEffect vfx = null;
    [SerializeField] string abBlendRef;

    [SerializeField] string gradientRefA;

    [SerializeField] string gradientRefB;

    [SerializeField] AnimationCurve curve;

    [SerializeField] float duration;

    bool interruted = false;

    public void BlendInGradient(Gradient gradient, float duration)
    {
        StartCoroutine(InterruptAndBlendInGradient(gradient, duration));
    }

    public void BlendInGradient(Gradient gradient)
    {
        StartCoroutine(InterruptAndBlendInGradient(gradient, duration));
    }

    IEnumerator InterruptAndBlendInGradient(Gradient gradient, float duration)
    {
        interruted = true;
        yield return new WaitForSeconds(0.1f);
        interruted = false;
        StartCoroutine(BlendInGradientR(gradient, duration));

    }

    IEnumerator BlendInGradientR(Gradient newGradient, float duration)
    {
        if (vfx == null)
        {
            Debug.LogError("VisualEffect is not set!");
            yield break;
        }

        float targetValue;
        float startValue;
        targetValue = (vfx.GetFloat(abBlendRef) >= 0.5f) ? 0 : 1f;
        startValue = (vfx.GetFloat(abBlendRef) >= 0.5f) ? 1f : 0;

        while (Mathf.Abs(vfx.GetFloat(abBlendRef) - startValue) > 0.01f && !interruted)
        {
            vfx.SetFloat(abBlendRef, Mathf.Lerp(vfx.GetFloat(abBlendRef), startValue, 0.1f));
            yield return null;
        }

        vfx.SetFloat(abBlendRef, startValue);

        if (startValue == 0)
        {
            vfx.SetGradient(gradientRefB, newGradient);
        }
        else
        {
            vfx.SetGradient(gradientRefA, newGradient);
        }

        float timer = 0;

        while (timer < duration && !interruted)
        {
            timer += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, curve.Evaluate(timer / duration));
            vfx.SetFloat(abBlendRef, newValue);
            yield return null;
        }

        yield break;

    }
}

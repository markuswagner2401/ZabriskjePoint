using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class VFXParameterAnimator : MonoBehaviour
{
    [SerializeField] UnityEvent doOnStart;

    [SerializeField] VisualEffect vfx;
    

    [SerializeField] FloatChanger[] floatChangers;

    [System.Serializable]
    struct FloatChanger
    {
        public string note;
        public string propRef;
        public float targetValue;

        public float randomRangeMin;
        public float randomRangeMax;
        public float duration;

        public float pushFactor;

        public bool playFloat;

        public float playSpeed;


        public AnimationCurve curve;
        public bool interrupted;

        public bool automaticlyPlayNext;

        public float waitTime;

        public bool playDaumenkino;

    }

    [System.Serializable]
    struct FloatUpdater
    {
        public string name;

        public float startValue;

        public string propRef;
        public float smoothing;


    }

    [SerializeField] FloatUpdater[] floatUpdaters;

    private void Awake() 
    {
        if(vfx == null)
        {
            vfx = GetComponent<VisualEffect>();
        }
    }
    void Start()
    {
        doOnStart.Invoke();
    }


    void Update()
    {

    }

    // Float Changer



    public void ChangeFloat(string name)
    {
        if(!gameObject.activeInHierarchy) return;
        int index = GetFloatChangerIndexOfName(name);

        if (index >= 0)
        {
            ChangeFloat(index);
        }

        else
        {
            print("no float changer with name " + name + " found at" + gameObject.name);

        }

    }

    public void ChangeFloat(int index)
    {
        if(!gameObject.activeInHierarchy) return;
        if (index < floatChangers.Length)

            StartCoroutine(InterruptAndChangeFloatR(index));
    }

    private int GetFloatChangerIndexOfName(string name)
    {
        for (int i = 0; i < floatChangers.Length; i++)
        {
            if (floatChangers[i].note == name)
            {
                return i;
            }
            else
            {
                continue;
            }
        }

        return -1;
    }



    IEnumerator InterruptAndChangeFloatR(int index)
    {
        //floatChangers[index].interrupted = true;
        InterruptFloatChangers(floatChangers[index].propRef);
        yield return new WaitForSecondsRealtime(0.01f);
        StartCoroutine(ChangeFloatR(index));
        yield break;
    }

    void InterruptFloatChangers(string refName)
    {
        for (int i = 0; i < floatChangers.Length; i++)
        {
            if (floatChangers[i].propRef == refName)
            {
                floatChangers[i].interrupted = true;
            }
        }
    }

    IEnumerator ChangeFloatR(int index)
    {

        floatChangers[index].interrupted = false;
        float timer = 0f;

        //       print("change float " + floatChangers[index].note);

        float startValue = vfx.GetFloat(floatChangers[index].propRef);

        while (timer <= floatChangers[index].duration && !floatChangers[index].interrupted)
        {

            timer += Time.unscaledDeltaTime;
            float newValue = Mathf.Lerp(startValue, floatChangers[index].targetValue, floatChangers[index].curve.Evaluate(timer / floatChangers[index].duration));
            vfx.SetFloat(floatChangers[index].propRef, newValue);

            yield return null;
        }

        if (floatChangers[index].automaticlyPlayNext)
        {
            yield return new WaitForSeconds(floatChangers[index].waitTime);
            ChangeFloat(index + 1);
        }


        yield break;
    }


    /// float Updater

    public void UpdateFloat(int index, float newValue)
        {
            if(!gameObject.activeInHierarchy) return;

            float currentValue = vfx.GetFloat(floatUpdaters[index].propRef);
            float smoothedValue = Mathf.Lerp(currentValue, newValue, floatUpdaters[index].smoothing);
            vfx.SetFloat(floatUpdaters[index].propRef, smoothedValue);
        }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.VFX;

[RequireComponent(typeof(HeightmapAnalysis))]
public class HeightmapEvaluation : MonoBehaviour
{
    [SerializeField] float resetDuration = 60f;

    float resetTimer = 0;
    [SerializeField] bool evaluationActive = true;
    [SerializeField] VideoControl videoControl = null;

    [SerializeField] TextMeshPro tmp;

    [SerializeField] VisualEffect vfx;
    [SerializeField] string textBlendRef;

    [SerializeField] float textBlendDuration;

    [SerializeField] AnimationCurve textBlendCurve;

    bool textBlendInterrupted = false;

    [SerializeField] Task[] tasks;

    [System.Serializable]
    public struct Task
    {
        public string name;

        [TextArea(3,10)]
        public string taskText;

        [Tooltip("-1 for not needed to fulfill task")]
        public int hills;

        [Tooltip("-1 for not needed to fulfill task")]
        public int troughs;

        [Tooltip("-1 for not needed to fulfill task")]
        public float lowestTrough;

        [Tooltip("-1 for not needed to fulfill task")]
        public float highestHill;

        [Tooltip("-1 for not needed to fulfill task")]
        public float minimalHeightDifference;

        [Tooltip("-1 for not needed to fulfill task")]
        public float maximalHeightDifference;


        public string videoAtSuccess;

        public GameObject hillIndicatorAtSuccess;
        public GameObject hillIndicatorAtNoSuccess;

        public GameObject troughIndicatorAtSuccess;
        public GameObject troughIndicatorAtNoSuccess;


    }
    public float waitBeforeAnalyze = 3f;

    bool evaluationInterrupted;

    bool resetTimerInterruted;

    HeightmapAnalysis heightmapAnalysis;

    public int currentTaskIndex;

    List<GameObject> spawnedIndicators = new List<GameObject>();


    void Start()
    {
        heightmapAnalysis = GetComponent<HeightmapAnalysis>();
        if (videoControl == null)
        {
            videoControl = FindObjectOfType<VideoControl>();
        }
    }



    // Update is called once per frame
    void Update()
    {

        // Timer


    }

    // Resetting

    public void StartResetTimer()
    {
        print("reset");
        StartCoroutine(InterruptAndRunResetTimer());
    }

    IEnumerator InterruptAndRunResetTimer()
    {
        resetTimerInterruted = true;
        yield return new WaitForSeconds(0.1f);
        resetTimerInterruted = false;
        StartCoroutine(ResetTimerRoutine());
        yield break;
    }

    IEnumerator ResetTimerRoutine()
    {
        float timer = 0;
        while (timer < resetDuration && !resetTimerInterruted)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (!resetTimerInterruted)
        {
            Reset();

        }
        yield break;
    }

    public void Reset()
    {
        currentTaskIndex = 0;
        videoControl.Integrate("Start");
    }

    ///

    public void EvaluatHeightmap()
    {
        if (!evaluationActive) return;

        print("evaluate heightmap");
        StartCoroutine(InterruptAndEvaluate());
    }

    public void ActivateEvaluation(bool value)
    {
        print("activate Evaluation: " + value);
        evaluationActive = value;

        if(value)
        {
            FadeInTaskText();
        }
        else
        {
            FadeOutText();
        }
    }

    public void InterrutEvaluation()
    {
        print("interrupt evaluation");
        evaluationInterrupted = true;
    }

    IEnumerator InterruptAndEvaluate()
    {
        evaluationInterrupted = true;
        yield return new WaitForSeconds(0.1f);
        evaluationInterrupted = false;
        StartCoroutine(EvaluationRoutine());
        yield break;
    }

    IEnumerator EvaluationRoutine()
    {
        float timer = 0;
        while (timer < waitBeforeAnalyze && !evaluationInterrupted)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        heightmapAnalysis.AnalyseHeightmap();

        yield return new WaitForSeconds(1f);

        if (evaluationInterrupted) yield break;

        int hillsCount = heightmapAnalysis.GetMainHillsCount();
        int troughsCount = heightmapAnalysis.GetMainTroughsCount();
        float heightestHillHeight = heightmapAnalysis.GetHeightOfHeighestHill();
        float lowestTroughHeight = heightmapAnalysis.GetHeightOfLowestTrough();

        bool taskFulfilled = EvaluateTask(hillsCount, troughsCount, heightestHillHeight, lowestTroughHeight);

        string nextVideoName = "";

        if (taskFulfilled)
        {
            
            nextVideoName = tasks[currentTaskIndex].videoAtSuccess;

            print("task fulfilled, next video " + nextVideoName);


            SpawnIndicators(true, currentTaskIndex);

            if (videoControl != null)
            {
                videoControl.Integrate(nextVideoName);
            }
            else
            {
                Debug.LogError("No Video Control Component found in Heightmap Evaluation");
            }

            currentTaskIndex++;
            currentTaskIndex %= tasks.Length;
        }
        else
        {
            print("task NOT  fulfilled");
            SpawnIndicators(false, currentTaskIndex);
        }





        yield break;
    }

    bool EvaluateTask(int hillsCount, int troughsCount, float heighestHillHeight, float lowestTroughHeight)
    {
        print("Evaluate Task: hills: " + hillsCount + " troughs: " + troughsCount + " heighestHill: " + heighestHillHeight + " lowestTrough: " + lowestTroughHeight);
        if (tasks[currentTaskIndex].hills >= 0)
        {
            if (hillsCount != tasks[currentTaskIndex].hills)
            {
                return false;
            }
        }

        if (tasks[currentTaskIndex].troughs >= 0)
        {
            if (troughsCount != tasks[currentTaskIndex].troughs)
            {
                return false;
            }
        }

        if (tasks[currentTaskIndex].highestHill >= 0)
        {
            if (tasks[currentTaskIndex].highestHill < heighestHillHeight)
            {
                return false;
            }
        }

        if (tasks[currentTaskIndex].lowestTrough >= 0)
        {
            if (tasks[currentTaskIndex].lowestTrough > lowestTroughHeight)
            {
                return false;
            }
        }

        if (tasks[currentTaskIndex].maximalHeightDifference >= 0)
        {
            if (tasks[currentTaskIndex].maximalHeightDifference > (Mathf.Abs(heighestHillHeight - lowestTroughHeight)))
            {
                return false;
            }
        }

        if (tasks[currentTaskIndex].minimalHeightDifference >= 0)
        {
            if (tasks[currentTaskIndex].minimalHeightDifference < (Mathf.Abs(heighestHillHeight - lowestTroughHeight)))
            {
                return false;
            }
        }



        return true;
    }

    void SpawnIndicators(bool success, int taskIndex)
    {
        // destorying
        if (spawnedIndicators != null)
        {
            foreach (var item in spawnedIndicators)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            spawnedIndicators.Clear();

        }





        if (success)
        {
            if (tasks[taskIndex].hillIndicatorAtSuccess != null)
            {
                Vector3[] positions = heightmapAnalysis.GetMainHillsPositions();

                foreach (var position in positions)
                {
                    GameObject indicator;

                    indicator = Instantiate(tasks[taskIndex].hillIndicatorAtSuccess, position, Quaternion.identity);
                    spawnedIndicators.Add(indicator);
                }
            }

            if (tasks[taskIndex].troughIndicatorAtSuccess != null)
            {
                Vector3[] positions = heightmapAnalysis.GetMainTroughsPositions();

                foreach (var position in positions)
                {
                    GameObject indicator;

                    indicator = Instantiate(tasks[taskIndex].troughIndicatorAtSuccess, position, Quaternion.identity);
                    spawnedIndicators.Add(indicator);
                }
            }
        }

        else
        {
            if (tasks[taskIndex].hillIndicatorAtNoSuccess != null)
            {
                Vector3[] positions = heightmapAnalysis.GetMainHillsPositions();

                foreach (var position in positions)
                {
                    GameObject indicator;

                    indicator = Instantiate(tasks[taskIndex].hillIndicatorAtNoSuccess, position, Quaternion.identity);
                    spawnedIndicators.Add(indicator);
                }
            }

            if (tasks[taskIndex].troughIndicatorAtNoSuccess != null)
            {
                Vector3[] positions = heightmapAnalysis.GetMainTroughsPositions();

                foreach (var position in positions)
                {
                    GameObject indicator;

                    indicator = Instantiate(tasks[taskIndex].troughIndicatorAtNoSuccess, position, Quaternion.identity);
                    spawnedIndicators.Add(indicator);
                }
            }
        }


    }



    public void FadeOutText()
    {
        StartCoroutine(InterruptAndFadeOut());
    }

    public void FadeInTaskText()
    {
        StartCoroutine(ChangeTextRoutine());
    }

    IEnumerator InterruptAndFadeOut()
    {
        print("fade out text");

        textBlendInterrupted = true;
        yield return new WaitForSeconds(0.01f);
        textBlendInterrupted = false;

        if (vfx.GetFloat(textBlendRef) > 0.001f)
        {
            StartCoroutine(FadeTextR(0, textBlendDuration));
            yield return new WaitForSeconds(1f);
        }

        yield break;
    }

    IEnumerator ChangeTextRoutine()
    {
        //yield return InterruptAndFadeOut();

        textBlendInterrupted = true;
        yield return new WaitForSeconds(0.01f);
        textBlendInterrupted = false;

        if (vfx.GetFloat(textBlendRef) > 0.001)
        {
            StartCoroutine(FadeTextR(0, textBlendDuration));
            yield return new WaitForSeconds(1f);
        }

        if (textBlendInterrupted) yield break;
        tmp.text = tasks[currentTaskIndex].taskText;

        print("Set Text: " + tasks[currentTaskIndex].taskText);

        StartCoroutine(FadeTextR(1f, textBlendDuration));

        yield break;
    }

    IEnumerator FadeTextR(float targetValue, float duration)
    {
        print("fade text: " + targetValue);

        float startValue = vfx.GetFloat(textBlendRef);
        float timer = 0;

        while (timer < duration && !textBlendInterrupted)
        {
            timer += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, textBlendCurve.Evaluate(timer / duration));
            //print("fading text: " + newValue);
            vfx.SetFloat(textBlendRef, newValue);
            yield return null;

        }

        yield break;
    }



}
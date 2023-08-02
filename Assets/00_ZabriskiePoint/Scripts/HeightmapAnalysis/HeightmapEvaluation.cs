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

        [TextArea(3, 10)]
        public string taskText;

        public float hillsMinHeight;

        [Tooltip("0 for not show hills, -1 for show all found hills")]
        public int showFirstXHills;

        public float troughsMaxHeight;

        [Tooltip("0 for not show troughs, -1 for show all found troughs")]
        public int showFirstXTroughs;



        [Tooltip("-1 for not needed to fulfill task")]
        public int minHills;

        [Tooltip("-1 for not needed to fulfill task")]
        public int maxHills;



        [Tooltip("-1 for not needed to fulfill task")]
        public int minTroughs;

        [Tooltip("-1 for not needed to fulfill task")]
        public int maxTroughs;

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

    [SerializeField] RippleShaderController rippleShaderController;

    public int currentTaskIndex;

    List<GameObject> spawnedIndicators = new List<GameObject>();


    void Start()
    {
        heightmapAnalysis = GetComponent<HeightmapAnalysis>();
        if (videoControl == null)
        {
            videoControl = FindObjectOfType<VideoControl>();
        }



        rippleShaderController = GetComponent<RippleShaderController>();

    }



    // Update is called once per frame
    void Update()
    {

        // Timer

        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowRipples(-1,-1,-1,-1);
            int hillsCount = heightmapAnalysis.GetMainHillsCount();
            int troughsCount = heightmapAnalysis.GetMainTroughsCount();
            float heightestHillHeight = heightmapAnalysis.GetHeightOfHeighestHill();
            float lowestTroughHeight = heightmapAnalysis.GetHeightOfLowestTrough();

            print("hills Count: " + hillsCount);
            print("troughs Count: " + troughsCount);
            print("heighest hill: " + heightestHillHeight);
            print("lowest trough: " + lowestTroughHeight);
            print("difference: " + Mathf.Abs(heightestHillHeight - lowestTroughHeight));


        }


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

    public void ActivateEvaluation(bool value)
    {
        // print("activate Evaluation: " + value);
        // evaluationActive = value;

        if (value)
        {
            FadeInTaskText();
            StartCoroutine(WaitAndActivateEvaluation(true, textBlendDuration + 1f));
        }
        else
        {
            evaluationActive = false;
            FadeOutText();
        }
    }

    IEnumerator WaitAndActivateEvaluation(bool value, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        print("activate Evaluation: " + value);
        evaluationActive = value;


        yield break;
    }

    public void InterrutEvaluation()
    {
        print("interrupt evaluation");
        evaluationInterrupted = true;
    }

    public void EvaluatHeightmap()
    {
        if (!evaluationActive) return;

        print("evaluate heightmap");
        StartCoroutine(InterruptAndEvaluate());
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

        if (evaluationInterrupted) yield break;

        heightmapAnalysis.AnalyseHeightmap();

        yield return new WaitForSeconds(1f);

        if (evaluationInterrupted) yield break;



        int hillsCount = heightmapAnalysis.GetMainHillsCount();
        int troughsCount = heightmapAnalysis.GetMainTroughsCount();
        float heightestHillHeight = heightmapAnalysis.GetHeightOfHeighestHill();
        float lowestTroughHeight = heightmapAnalysis.GetHeightOfLowestTrough();

        bool taskFulfilled = EvaluateTask(hillsCount, troughsCount, heightestHillHeight, lowestTroughHeight);

        string nextVideoName = "";

        ShowRipples(tasks[currentTaskIndex].showFirstXHills, tasks[currentTaskIndex].hillsMinHeight, tasks[currentTaskIndex].showFirstXTroughs, tasks[currentTaskIndex].troughsMaxHeight);

        if (taskFulfilled)
        {

            nextVideoName = tasks[currentTaskIndex].videoAtSuccess;

            print("task fulfilled, next video " + nextVideoName);


            //SpawnIndicators(true, currentTaskIndex);





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
            //SpawnIndicators(false, currentTaskIndex);
        }





        yield break;
    }

    bool EvaluateTask(int hillsCount, int troughsCount, float heighestHillHeight, float lowestTroughHeight)
    {
        print("Evaluate Task: hills: " + hillsCount + " troughs: " + troughsCount + " heighestHill: " + heighestHillHeight + " lowestTrough: " + lowestTroughHeight);
        if (tasks[currentTaskIndex].minHills >= 0)
        {
            if (hillsCount < tasks[currentTaskIndex].minHills)
            {
                print("Not Enough hills: task not fulfilled");
                return false;
            }
        }

        if (tasks[currentTaskIndex].maxHills >= 0)
        {
            if (hillsCount > tasks[currentTaskIndex].maxHills)
            {
                print("Too many hills: task not fulfilled");
                return false;
            }
        }

        if (tasks[currentTaskIndex].minTroughs >= 0)
        {
            if (troughsCount < tasks[currentTaskIndex].minTroughs)
            {
                print("not enough troughs: task not fulfilled");
                return false;
            }
        }

        if (tasks[currentTaskIndex].maxTroughs >= 0)
        {
            if (troughsCount > tasks[currentTaskIndex].maxTroughs)
            {
                print("too many troughs: task not fulfilled");
                return false;
            }
        }

        if (tasks[currentTaskIndex].highestHill >= 0)
        {
            if (heighestHillHeight < tasks[currentTaskIndex].highestHill)
            {
                print("heighest hill not heigh enough: taks not fulfulled");
                return false;
            }
        }

        if (tasks[currentTaskIndex].lowestTrough >= 0)
        {
            if (lowestTroughHeight > tasks[currentTaskIndex].lowestTrough)
            {
                print("lowest trough not deep enough: task not fulfilled");
                return false;
            }
        }

        if (tasks[currentTaskIndex].maximalHeightDifference >= 0)
        {
            if ((Mathf.Abs(heighestHillHeight - lowestTroughHeight)) > tasks[currentTaskIndex].maximalHeightDifference)
            {
                print("difference from heighest hill to lowest trough too big: " + (Mathf.Abs(heighestHillHeight - lowestTroughHeight)) + " : task not fulfilled");
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

    void ShowRipples(int hillsCount, float hillsMinHeight, int troughsCount, float troughsMaxHeight)
    {
        print("show ripples");
        List<Vector3> validHills = new List<Vector3>();
        Vector2[] newHillsPositions = new Vector2[0];
        List<Vector3> validTroughs = new List<Vector3>();
        Vector2[] newTroughsPositions = new Vector2[0];

        if (hillsCount != 0 && heightmapAnalysis.GetMainHillsTexturePositionsAndHeights() != null)
        {
            foreach (var item in heightmapAnalysis.GetMainHillsTexturePositionsAndHeights())
            {
                validHills.Add(item);
            }

            // sort list by y value
            validHills.Sort((v1, v2) => v2.y.CompareTo(v1.y));

            // filter by y value
            if (hillsMinHeight != -1)
            {
                validHills.RemoveAll(item => item.y <= hillsMinHeight);
            }

            // filter by count
            if (hillsCount != -1 && hillsCount < validHills.Count)
            {
                validHills.RemoveRange(hillsCount, validHills.Count - hillsCount);
            }

            newHillsPositions = new Vector2[validHills.Count];

            for (int i = 0; i < validHills.Count; i++)
            {
                Vector2 position = new Vector2();
                position.x = validHills[i].x;
                position.y = validHills[i].z;
                newHillsPositions[i] = position;
            }
        }

        if (troughsCount != 0 && heightmapAnalysis.GetMainTroughsTexturePositionsAndHeights() != null)
        {
            foreach (var item in heightmapAnalysis.GetMainTroughsTexturePositionsAndHeights())
            {
                validTroughs.Add(item);
            }

            // sort list by y value
            validTroughs.Sort((v1, v2) => v2.y.CompareTo(v1.y));

            // filter by y value
            if (troughsMaxHeight != -1)
            {
                validTroughs.RemoveAll(item => 1f - item.y >= troughsMaxHeight);
            }

            // filter by count
            if (troughsCount != -1 && troughsCount < validTroughs.Count)
            {
                validTroughs.RemoveRange(troughsCount, validTroughs.Count - troughsCount);
            }

            newTroughsPositions = new Vector2[validTroughs.Count];

            for (int i = 0; i < validTroughs.Count; i++)
            {
                Vector2 position = new Vector2();
                position.x = validTroughs[i].x;
                position.y = validTroughs[i].z;
                newTroughsPositions[i] = position;
            }
        }

        rippleShaderController.CreateRipples(newHillsPositions, newTroughsPositions);
    }


    // Text

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

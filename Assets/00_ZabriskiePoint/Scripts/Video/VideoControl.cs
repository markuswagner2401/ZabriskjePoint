using System.Collections;
using UnityEngine;
using UnityEngine.Video;



public class VideoControl : MonoBehaviour
{


    public enum SequenceBehaviour
    {
        PlayFirstVideoOfSequence,
        PlayRandomVideoOfSequence,
        PlayNextVideoOfSequence,
        PlayNamedVideoOfSequence
    }




    public enum ClipEndBehaviour
    {
        PlayOnce,
        PlayNext,
        Loop
    }


    [System.Serializable]
    public struct VideoFader
    {
        public string name;
        public VideoClip videoClip;
        public float fadeInDuration;
        public AnimationCurve animationCurve;
        public ClipEndBehaviour clipEndBehaviour;


    }

    [System.Serializable]
    public struct VideoSequence
    {
        public string name;
        public VideoFader[] videoFaders;
        public int currentFaderIndex;
    }



    [SerializeField] private int currentSequenceIndex = 0;

    [SerializeField] private VideoSequence[] videoSequences;

    public Material VideoShader;
    public VideoPlayer videoPlayerA;
    public VideoPlayer videoPlayerB;


    private bool isBlendingTransition;

    int targetBlend = 0; // 0 for targeting VideoPlayerA, 1 for targeting VideoPlayerB

    bool interrupted = false;


    /// public methods (to be used by UnityEvents, therefore without PlayBehaviour enum)


    // public void PlayRandomVideoOfSequence(string sequenceName)
    // {
    //     int sequenceIndex = GetSequenceIndexByName(sequenceName);
    //     if (sequenceIndex != -1)
    //     {
    //         PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayRandomVideoOfSequence, string.Empty);
    //     }
    // }

    // public void PlayNextVideoOfSequence(string sequenceName)
    // {
    //     int sequenceIndex = GetSequenceIndexByName(sequenceName);
    //     if (sequenceIndex != -1)
    //     {   
    //         PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence, string.Empty);
    //     }
    // }

    // public void PlayFirstVideoOfSequence(string sequenceName)
    // {
    //     int sequenceIndex = GetSequenceIndexByName(sequenceName);
    //     if (sequenceIndex != -1)
    //     {
    //         PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayFirstVideoOfSequence, string.Empty);
    //     }
    // }

    

    public void Integrate()
    {
        SetNextSequence();
        PlayFirstVideoOfCurrentSequence();
    }

    public void Desintegrate()
    {
        PlayNamedFaderOfCurrentSequence("Outro");
    }

    //

    public void SetCurrentSequence(string name)
    {
        currentSequenceIndex = GetSequenceIndexByName(name);
    }

    public void SetNextSequence()
    {
        currentSequenceIndex++;
        currentSequenceIndex %= videoSequences.Length;
    }


    public void PlayFirstVideoOfCurrentSequence()
    {
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayFirstVideoOfSequence, string.Empty);
    }

    public void PlayNextVideoOfCurrentSequence()
    {
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence, string.Empty);
    }

    public void PlayNamedFaderOfCurrentSequence(string name)
    {
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayNamedVideoOfSequence, name);
    }







    //// private methods

    private int GetSequenceIndexByName(string name)
    {
        for (int i = 0; i < videoSequences.Length; i++)
        {
            if (videoSequences[i].name == name)
            {
                return i;
            }
        }
        Debug.LogError($"VideoSequence with name '{name}' not found!");
        return -1;
    }

    private int GetFaderIndexByName(int sequenceIndex, string name)
    {
        for (int i = 0; i < videoSequences[sequenceIndex].videoFaders.Length; i++)
        {
            if (videoSequences[sequenceIndex].videoFaders[i].name == name)
            {
                return i;
            }
        }
        Debug.LogError($"VideoFader with name '{name}' not found!");
        return -1;

    }




    // video group methods

    private void PlayVideoOfSequence(int sequenceIndex, SequenceBehaviour playBehaviour, string name)
    {
        if (sequenceIndex < 0 || sequenceIndex >= videoSequences.Length)
        {
            Debug.LogError("Invalid VideoChangerGroup index!");
            return;
        }

        int faderIndex = 0;
        switch (playBehaviour)
        {
            case SequenceBehaviour.PlayFirstVideoOfSequence:
                faderIndex = 0;
                break;

            case SequenceBehaviour.PlayRandomVideoOfSequence:
                faderIndex = Random.Range(0, videoSequences[sequenceIndex].videoFaders.Length);
                break;

            case SequenceBehaviour.PlayNextVideoOfSequence:
                faderIndex = (videoSequences[sequenceIndex].currentFaderIndex + 1) % videoSequences[sequenceIndex].videoFaders.Length;
                break;

            case SequenceBehaviour.PlayNamedVideoOfSequence:
                faderIndex = GetFaderIndexByName(sequenceIndex, name);
                break;

            default:
                faderIndex = 0;
                break;
        }

        currentSequenceIndex = sequenceIndex;
        videoSequences[currentSequenceIndex].currentFaderIndex = faderIndex;

        BlendInClip(sequenceIndex, faderIndex);

    }




    ////

    private void BlendInClip(int sequenceIndex, int faderIndex)
    {

        StartCoroutine(InterruptAndBlendInClip(sequenceIndex, faderIndex));

    }

    IEnumerator InterruptAndBlendInClip(int sequenceIndex, int faderIndex)
    {
        //StopAllCoroutines();
        interrupted = true;
        yield return new WaitForSeconds(0.2f);
        interrupted = false;
        StartCoroutine(BlendingTransitionRoutine(sequenceIndex, faderIndex));
        yield break;
    }



    IEnumerator BlendingTransitionRoutine(int sequenceIndex, int faderIndex)
    {

        float currentBlend = VideoShader.GetFloat("_BlendAB");
        //float targetBlend = currentBlend >= 0.5f ? 1f : 0f;


        while ((Mathf.Abs(targetBlend - VideoShader.GetFloat("_BlendAB")) > 0.01f) && !interrupted)
        {
            float value = Mathf.MoveTowards(VideoShader.GetFloat("_BlendAB"), targetBlend, Time.deltaTime);
            VideoShader.SetFloat("_BlendAB", value);
            yield return null;
        }

        if (interrupted) yield break;

        VideoShader.SetFloat("_BlendAB", targetBlend);
        isBlendingTransition = false;
        StartBlendInClip(sequenceIndex, faderIndex);
        yield break;
    }

    private void StartBlendInClip(int sequenceIndex, int faderIndex)
    {

        float currentBlend = VideoShader.GetFloat("_BlendAB");

        // Depending on the current blend, set next clip to either ClipA or ClipB
        if (currentBlend >= 0.9f)
        {
            videoPlayerA.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerA.Prepare();
            videoPlayerA.time = 0f;
            targetBlend = 0;
            if (!interrupted)
            {
                StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, 0f));
            }

        }
        else
        {
            videoPlayerB.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerB.Prepare();
            targetBlend = 1;
            videoPlayerB.time = 0f;
            if (!interrupted)
            {
                StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, 1f));
            }

        }
    }





    IEnumerator BlendingRoutine(int sequenceIndex, int faderIndex, float startValue, float endValue)
    {
        //print("Blending Routine: seq:" + sequenceIndex + "faderIndex: " + faderIndex);

        float time = 0f;
        float duration = videoSequences[sequenceIndex].videoFaders[faderIndex].fadeInDuration;
        AnimationCurve curve = videoSequences[sequenceIndex].videoFaders[faderIndex].animationCurve;

        double clipLength = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip.length;

        VideoPlayer currentVideoPlayer = (endValue == 1f) ? videoPlayerB : videoPlayerA;

        currentVideoPlayer.Play();



        while ((time < duration) && !interrupted)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(startValue, endValue, curve.Evaluate(time / duration));
            VideoShader.SetFloat("_BlendAB", value);

            yield return null;
        }

        if(!interrupted)
        {
            VideoShader.SetFloat("_BlendAB", endValue);
        }

        else
        {
            yield break;
        }

        

        // clip end behaviour

        if (videoSequences[sequenceIndex].videoFaders[faderIndex].clipEndBehaviour == ClipEndBehaviour.PlayNext)
        {
            int nextFaderIndex = (faderIndex + 1) % videoSequences[sequenceIndex].videoFaders.Length;
            float nextFadeInTime = videoSequences[sequenceIndex].videoFaders[nextFaderIndex].fadeInDuration;
            double lastVideoPlayerTime = 0;

            while ((currentVideoPlayer.time < clipLength - nextFadeInTime || currentVideoPlayer.time != lastVideoPlayerTime) && !interrupted)
            {
                lastVideoPlayerTime = currentVideoPlayer.time;
                yield return null;
            }
            if (interrupted) yield break;
            //print("play Next behaviour");
            PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence, "");

        }

        else if (videoSequences[sequenceIndex].videoFaders[faderIndex].clipEndBehaviour == ClipEndBehaviour.Loop)
        {
            currentVideoPlayer.isLooping = true;
        }

        else if (videoSequences[sequenceIndex].videoFaders[faderIndex].clipEndBehaviour == ClipEndBehaviour.PlayOnce)
        {
            currentVideoPlayer.isLooping = false;

        }

        yield break;


    }


}
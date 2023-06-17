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

    private int currentSequenceIndex = 0;

    [SerializeField] private VideoSequence[] videoSequences;

    public Material VideoShader;
    public VideoPlayer videoPlayerA;
    public VideoPlayer videoPlayerB;

    private Coroutine blendCoroutine;
    private bool isBlendingTransition;


    /// public methods (to be used by UnityEvents, therefore without PlayBehaviour enum)


    public void PlayRandomVideoOfSequence(string sequenceName)
    {
        int sequenceIndex = GetSequenceIndexByName(sequenceName);
        if (sequenceIndex != -1)
        {
            PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayRandomVideoOfSequence);
        }
    }

    public void PlayNextVideoOfSequence(string sequenceName)
    {
        int sequenceIndex = GetSequenceIndexByName(sequenceName);
        if (sequenceIndex != -1)
        {   
            PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence);
        }
    }

    public void PlayFirstVideoOfSequence(string sequenceName)
    {
        int sequenceIndex = GetSequenceIndexByName(sequenceName);
        if (sequenceIndex != -1)
        {
            PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayFirstVideoOfSequence);
        }
    }

    public void PlayFirstVideoOfCurrentSequence()
    {
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayFirstVideoOfSequence);
    }

    public void PlayNextVideoOfCurrentSequence()
    {
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence);
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
        Debug.LogError($"VideoChangerGroup with name '{name}' not found!");
        return -1;
    }




    // video group methods

    private void PlayVideoOfSequence(int sequenceIndex, SequenceBehaviour playBehaviour)
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


        // If a blending routine is currently running, stop it and complete the ongoing blending quickly
        if (blendCoroutine != null)
        {
            isBlendingTransition = true;
            blendCoroutine = StartCoroutine(BlendingTransitionRoutine(sequenceIndex, faderIndex));
            return;
        }

        StartBlendInClip(sequenceIndex, faderIndex);
    }

    IEnumerator BlendingTransitionRoutine(int sequenceIndex, int faderIndex)
    {

        float currentBlend = VideoShader.GetFloat("_BlendAB");
        float targetBlend = currentBlend >= 0.5f ? 1f : 0f;
        

        while (Mathf.Abs(targetBlend - VideoShader.GetFloat("_BlendAB")) > 0.01f)
        {
            float value = Mathf.MoveTowards(VideoShader.GetFloat("_BlendAB"), targetBlend, Time.deltaTime);
            VideoShader.SetFloat("_BlendAB", value);

            yield return null;
        }

        VideoShader.SetFloat("_BlendAB", targetBlend);
        isBlendingTransition = false;

        StartBlendInClip(sequenceIndex, faderIndex);
    }

    private void StartBlendInClip(int sequenceIndex, int faderIndex)
    {

        float currentBlend = VideoShader.GetFloat("_BlendAB");

        // Depending on the current blend, set next clip to either ClipA or ClipB
        if (currentBlend >= 1f)
        {
            videoPlayerA.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerA.Prepare();
            videoPlayerA.time = 0f;
            blendCoroutine = StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, 0f));
        }
        else
        {
            videoPlayerB.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerB.Prepare();
            videoPlayerB.time = 0f;
            blendCoroutine = StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, 1f));
        }
    }

    

    

    IEnumerator BlendingRoutine(int sequenceIndex, int faderIndex, float startValue, float endValue)
    {

        float time = 0f;
        float duration = videoSequences[sequenceIndex].videoFaders[faderIndex].fadeInDuration;
        AnimationCurve curve = videoSequences[sequenceIndex].videoFaders[faderIndex].animationCurve;

        double clipLength = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip.length;

        VideoPlayer currentVideoPlayer = (endValue == 1f) ? videoPlayerB : videoPlayerA;

        currentVideoPlayer.Play();
        
        

        while (time < duration && !isBlendingTransition)
        {
            float value = Mathf.Lerp(startValue, endValue, curve.Evaluate(time / duration));
            VideoShader.SetFloat("_BlendAB", value);

            time += Time.deltaTime;
            yield return null;
        }

        if (!isBlendingTransition)
        {
            VideoShader.SetFloat("_BlendAB", endValue);
            blendCoroutine = null;
        }

        // clip end behaviour

        

        if(videoSequences[sequenceIndex].videoFaders[faderIndex].clipEndBehaviour == ClipEndBehaviour.PlayNext)
        {
            
            while (currentVideoPlayer.time < clipLength)
            {
                yield return null;
            }
            PlayVideoOfSequence(sequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence);
            
        }

        else if(videoSequences[sequenceIndex].videoFaders[faderIndex].clipEndBehaviour == ClipEndBehaviour.Loop)
        {
            currentVideoPlayer.isLooping = true;
        }

        else if(videoSequences[sequenceIndex].videoFaders[faderIndex].clipEndBehaviour == ClipEndBehaviour.PlayOnce)
        {
            currentVideoPlayer.isLooping = false;
            
        }

        yield break;


    }


}
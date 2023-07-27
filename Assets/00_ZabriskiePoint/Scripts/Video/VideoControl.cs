using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;



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

        public AudioClip audioClip;

        public float fadeInDurationVideo;

        public float fadeInDurationAudio;
        public AnimationCurve animationCurve;
        public ClipEndBehaviour clipEndBehaviour;

        public UnityEvent doOnClipEnd;


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

    public AudioSource audioSourceA;
    public AudioSource audioSourceB;


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



    // public void Integrate()
    // {
    //     SetNextSequence();
    //     PlayFirstVideoOfCurrentSequence();
    // }

    public void Integrate(string sequenceName) // gets called by HeightmapEvaluation
    {
        int nextIndex = -1;

        if (sequenceName != "")
        {
            nextIndex = GetSequenceIndexByName(sequenceName);
        }

        if (nextIndex >= 0)
        {
            currentSequenceIndex = nextIndex;
        }
        PlayFirstVideoOfCurrentSequence();
    }

    public void Desintegrate()
    {
        PlayNamedFaderOfCurrentSequence("Outro");
    }

    //

    public void SetCurrentSequence(string name)
    {
        int index = GetSequenceIndexByName(name);
        if (index >= 0)
        {
            currentSequenceIndex = index;
        }

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

        bool changeAudio = false;
        // Depending on the current blend, set next clip to either ClipA or ClipB
        if (currentBlend >= 0.9f)
        {
            // audio
            AudioClip nextClip = videoSequences[sequenceIndex].videoFaders[faderIndex].audioClip;
            if (nextClip != null)
            {
                changeAudio = true;
                audioSourceA.clip = nextClip;
                audioSourceA.time = 0f;

            }


            // video
            videoPlayerA.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerA.Prepare();
            videoPlayerA.time = 0f;
            targetBlend = 0;

            //
            if (!interrupted)
            {
                if (changeAudio)
                {
                    StartCoroutine(AudioBlendingRoutine(sequenceIndex, faderIndex, currentBlend, targetBlend));
                }

                StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, targetBlend));
            }

        }
        else
        {
            // audio
            AudioClip nextClip = videoSequences[sequenceIndex].videoFaders[faderIndex].audioClip;
            if (nextClip != null)
            {
                changeAudio = true;
                audioSourceB.clip = nextClip;
                audioSourceB.time = 0f;
            }



            // video
            videoPlayerB.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerB.Prepare();
            targetBlend = 1;
            videoPlayerB.time = 0f;

            //
            if (!interrupted)
            {
                if(changeAudio)
                {
                    StartCoroutine(AudioBlendingRoutine(sequenceIndex, faderIndex, currentBlend, targetBlend));
                }
                
                StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, targetBlend));
            }

        }
    }





    IEnumerator BlendingRoutine(int sequenceIndex, int faderIndex, float startValue, float endValue)
    {


        float time = 0f;
        float duration = videoSequences[sequenceIndex].videoFaders[faderIndex].fadeInDurationVideo;
        AnimationCurve curve = videoSequences[sequenceIndex].videoFaders[faderIndex].animationCurve;
        double clipLength = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip.length;

        // video
        VideoPlayer currentVideoPlayer = (endValue == 1f) ? videoPlayerB : videoPlayerA;
        currentVideoPlayer.Play();

        // audio
        AudioSource currentAudioSource = (endValue == 1f) ? audioSourceB : audioSourceA;
        currentAudioSource.Play();



        while ((time < duration) && !interrupted)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(startValue, endValue, curve.Evaluate(time / duration));
            VideoShader.SetFloat("_BlendAB", value);

            yield return null;
        }

        if (!interrupted)
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
            float nextFadeInTime = videoSequences[sequenceIndex].videoFaders[nextFaderIndex].fadeInDurationVideo;
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

    IEnumerator AudioBlendingRoutine(int sequenceIndex, int faderIndex, float startValue, float endValue)
    {
        float time = 0f;
        float duration = videoSequences[sequenceIndex].videoFaders[faderIndex].fadeInDurationAudio;
        AnimationCurve curve = videoSequences[sequenceIndex].videoFaders[faderIndex].animationCurve;

        AudioSource sourceA = (startValue == 0f) ? audioSourceA : audioSourceB;
        AudioSource sourceB = (startValue == 0f) ? audioSourceB : audioSourceA;

        while (time < duration && !interrupted)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(startValue, endValue, curve.Evaluate(time / duration));
            sourceA.volume = 1f - value;
            sourceB.volume = value;

            yield return null;
        }

        if (!interrupted)
        {
            sourceA.volume = 1f - endValue;
            sourceB.volume = endValue;
        }

        // Handle clip end behaviour the same way as in BlendingRoutine.
    }


}
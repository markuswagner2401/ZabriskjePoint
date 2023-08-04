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



        public float fadeInDurationVideo;


        public AnimationCurve animationCurve;
        public ClipEndBehaviour clipEndBehaviour;



    }

    [System.Serializable]
    public struct VideoSequence
    {
        public string name;

        public AudioClip audioClip;



        public AnimationCurve audioBlendCurve;

        public float audioFadeDuration;

        public UnityEvent doOnAudioStart;

        public UnityEvent doOnAudioEnd;

        public Gradient gradient;
        public VideoFader[] videoFaders;
        public int currentFaderIndex;
    }





    [SerializeField] private int currentSequenceIndex = 0;

    [SerializeField] private VideoSequence[] videoSequences;

    //public Material VideoShader;

    public Material[] videoMaterials;
    public VideoPlayer videoPlayerA;
    public VideoPlayer videoPlayerB;

    public AudioSource audioSourceA;
    public AudioSource audioSourceB;

    public VFXGradientBlender vFXGradientBlender;

    private bool isBlendingTransition;

    int targetBlend = 0; // 0 for targeting VideoPlayerA, 1 for targeting VideoPlayerB

    bool interrupted = false;

    bool audioInterrupted = false;

    // for evaluation activation

    bool thematicAudioRunning = false;

    // default audio

    public AudioClip defaultAudio;

    public AudioSource defaultSoundAudioSource;

    public float defaultSoundFadeDuration;

    public AnimationCurve defaultSoundFadeCurve;

    public UnityEvent doOnDefaultSoundStart; // for reset timer

    bool defaultSoundBlendInterrupted = false;




    public bool GetThematicAudioRunning()
    {
        return thematicAudioRunning;
    }

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
        //PlayNamedFaderOfCurrentSequence("Outro");
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
        print("set next video sequence index");
        currentSequenceIndex++;
        currentSequenceIndex %= videoSequences.Length;
    }


    public void PlayFirstVideoOfCurrentSequence()
    {
        print("Play first video of current sequence: sequence: " + currentSequenceIndex);
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayFirstVideoOfSequence, string.Empty);
        StartCoroutine(AudioBlendingRoutine(currentSequenceIndex));
        vFXGradientBlender.BlendInGradient(videoSequences[currentSequenceIndex].gradient);
    }

    public void PlayNextVideoOfCurrentSequence()
    {
        print("play next video of current sequence");
        PlayVideoOfSequence(currentSequenceIndex, SequenceBehaviour.PlayNextVideoOfSequence, string.Empty);
    }

    public void PlayNamedFaderOfCurrentSequence(string name)
    {
        print("play named fader of current sequence");
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
        yield return new WaitForSeconds(0.1f);
        interrupted = false;
        StartCoroutine(BlendingTransitionRoutine(sequenceIndex, faderIndex));
        yield break;
    }



    IEnumerator BlendingTransitionRoutine(int sequenceIndex, int faderIndex)
    {

        float currentBlend = videoMaterials[0].GetFloat("_BlendAB");


        //float targetBlend = currentBlend >= 0.5f ? 1f : 0f;

        foreach (var item in videoMaterials)
        {
            item.SetFloat("_BlendAB", currentBlend);
        }




        while ((Mathf.Abs(targetBlend - videoMaterials[0].GetFloat("_BlendAB")) > 0.01f) && !interrupted)
        {
            float value = Mathf.MoveTowards(videoMaterials[0].GetFloat("_BlendAB"), targetBlend, Time.deltaTime);
            foreach (var item in videoMaterials)
            {
                item.SetFloat("_BlendAB", value);
            }

            yield return null;
        }

        if (interrupted) yield break;

        foreach (var item in videoMaterials)
        {
            item.SetFloat("_BlendAB", targetBlend);
        }

        isBlendingTransition = false;
        StartBlendInClip(sequenceIndex, faderIndex);
        yield break;
    }

    private void StartBlendInClip(int sequenceIndex, int faderIndex)
    {

        float currentBlend = videoMaterials[0].GetFloat("_BlendAB");


        // Depending on the current blend, set next clip to either ClipA or ClipB
        if (currentBlend >= 0.9f)
        {



            // video
            videoPlayerA.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerA.Prepare();
            videoPlayerA.time = 0f;
            targetBlend = 0;

            //
            if (!interrupted)
            {


                StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, targetBlend));
            }

        }
        else
        {




            // video
            videoPlayerB.clip = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip;
            videoPlayerB.Prepare();
            targetBlend = 1;
            videoPlayerB.time = 0f;

            //
            if (!interrupted)
            {


                StartCoroutine(BlendingRoutine(sequenceIndex, faderIndex, currentBlend, targetBlend));
            }

        }
    }





    IEnumerator BlendingRoutine(int sequenceIndex, int faderIndex, float startValue, float endValue)
    {
        print("Start video blending" + videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip.name);

        float time = 0f;
        float duration = videoSequences[sequenceIndex].videoFaders[faderIndex].fadeInDurationVideo;

        AnimationCurve curve = videoSequences[sequenceIndex].videoFaders[faderIndex].animationCurve;
        double clipLength = videoSequences[sequenceIndex].videoFaders[faderIndex].videoClip.length;

        VideoPlayer currentVideoPlayer = (endValue == 1f) ? videoPlayerB : videoPlayerA;
        currentVideoPlayer.Play();

        if (duration == 0)
        {
            foreach (var item in videoMaterials)
            {
                item.SetFloat("_BlendAB", endValue);
            }
            
        }

        while ((time < duration) && !interrupted)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(startValue, endValue, curve.Evaluate(time / duration));
            foreach (var item in videoMaterials)
            {
                item.SetFloat("_BlendAB", value);
            }

            yield return null;
        }

        if (!interrupted)
        {
            foreach (var item in videoMaterials)
            {
                item.SetFloat("_BlendAB", endValue);
            }
        }
        else
        {
            yield break;
        }



        // clip end behaviour

        //videoSequences[sequenceIndex].videoFaders[faderIndex].doOnAudioClipEnd.Invoke();

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

    IEnumerator InterruptAndStartAudio()
    {
        audioInterrupted = true;
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(AudioBlendingRoutine(currentSequenceIndex));
        yield break;
    }



    IEnumerator AudioBlendingRoutine(int sequenceIndex)
    {
        videoSequences[sequenceIndex].doOnAudioStart.Invoke();

        thematicAudioRunning = true;

        FadeDefaultSound(0f);

        print("do on audiostart");


        float time = 0f;
        float duration = videoSequences[sequenceIndex].audioFadeDuration;
        AnimationCurve curve = videoSequences[sequenceIndex].audioBlendCurve;



        AudioSource sourceA = (audioSourceA.volume < 0.1f) ? audioSourceA : audioSourceB;
        AudioSource sourceB = (audioSourceA.volume < 0.1f) ? audioSourceB : audioSourceA;

        float startValue = sourceA.volume;
        float endValue = 1f;

        sourceB.clip = videoSequences[sequenceIndex].audioClip;

        sourceA.Play();
        sourceB.Play();



        if (sourceB.volume < 0.01f) sourceB.time = 0;



        while (time < duration && !audioInterrupted)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(startValue, endValue, curve.Evaluate(time / duration));
            sourceA.volume = 1f - value;
            sourceB.volume = value;

            yield return null;
        }

        if (!audioInterrupted)
        {
            sourceA.volume = 1f - endValue;
            sourceB.volume = endValue;
        }

        float remainingTime = sourceB.clip.length - sourceB.time;

        float timer = 0;
        while (timer < remainingTime && !audioInterrupted)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (!audioInterrupted)
        {
            print("do on audio End");
            videoSequences[sequenceIndex].doOnAudioEnd.Invoke();
            thematicAudioRunning = false;
            FadeDefaultSound(1f);
        }

        // Handle clip end behaviour the same way as in BlendingRoutine.
    }

    // default sound blending

    void FadeDefaultSound(float targetValue)
    {
        StartCoroutine(InterruptAndFadeDefaultSound(targetValue));
        doOnDefaultSoundStart.Invoke();
    }

    IEnumerator InterruptAndFadeDefaultSound(float targetValue)
    {
        defaultSoundBlendInterrupted = true;
        yield return new WaitForSeconds(0.1f);
        defaultSoundBlendInterrupted = false;
        StartCoroutine(FadeDefaultSoundR(targetValue));
        yield break;
    }

    IEnumerator FadeDefaultSoundR(float targetValue)
    {
        float timer = 0;
        float startValue = defaultSoundAudioSource.volume;
        while (timer < defaultSoundFadeDuration && !defaultSoundBlendInterrupted)
        {
            timer += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, defaultSoundFadeCurve.Evaluate(timer / defaultSoundFadeDuration));
            defaultSoundAudioSource.volume = newValue;
            yield return null;
        }
        yield break;
    }




}
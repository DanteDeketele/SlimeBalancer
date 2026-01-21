using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : BaseManager
{
   
   // General Sounds
    public AudioClip mainTheme;

    public AudioClip GameOverSound;

    public AudioClip GameSelectSound;

    public AudioClip UISelectSound;

    public AudioClip CountdownBeepSound;

    public AudioClip CountdownGoSound;


    //Tilt It Game
    public AudioClip TiltGameScoreSound;

    public AudioClip TiltItMainTheme;



    //Slime Ski Game
    public AudioClip SlimeSkieMainTheme;

    public AudioClip SkiGameScoreSound;

    public AudioClip SkiGameCrashSound;


    //Snake Game

    public AudioClip SnakeMainTheme;

    public AudioClip SnakeGrowSound;

    public AudioClip SnakeWallHitSound;


    //Volume control

    private float volume = 1.0f;


    // Audio source management

    private List<AudioSource> audioSources = new List<AudioSource>();
    private List<AudioSource> audioSourcesToDestroy = new List<AudioSource>();

    void Awake()
    {

        
    }

    public void ChangeVolume(float volume)
    {
        this.volume = volume;
        foreach (AudioSource source in audioSources)
        {
                source.volume = volume;
        }
    }

    private IEnumerator WaitForOtherToFinishAndPlay(AudioClip clip, bool loop, bool single)
    {
        int soundEffectsPlaying;
        do
        {
            soundEffectsPlaying = 0;
            foreach (AudioSource source in audioSources)
            {
                if (!source.loop && source.isPlaying)
                {
                    soundEffectsPlaying++;
                }
            }
            yield return null;
        } while (soundEffectsPlaying > 0);
        PlaySound(clip, loop, single);
    }
   

    public void PlaySound(AudioClip clip, bool loop = false, bool single = false, bool waitForOtherToFinish = false)
    {
        if (waitForOtherToFinish && audioSources.Count > 0)
        {
            StartCoroutine(WaitForOtherToFinishAndPlay(clip, loop, single));
            return;
        }

        if (single)
        {
            foreach (AudioSource source in audioSources)
            {
                if (source.clip == clip)
                {
                    // Sound is already playing
                    Debug.Log("Sound " + clip.name + " is already playing, not playing again.");
                    return;
                }
            }
        }

        GameObject audioSourceObject = new GameObject("Soundtrack_" + clip.name);
        DontDestroyOnLoad(audioSourceObject);
        AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
        audioSource.volume = volume;
        audioSources.Add(audioSource);
    }

    public void StopSound(AudioClip clip)
    {
        foreach (AudioSource source in audioSources)
        {
            if (source.clip == clip)
            {
                source.Stop();
                audioSourcesToDestroy.Add(source);
                Debug.Log("Stopped sound: " + clip.name);
            }
        }
    }

    public void StopAllMusic()
    {
        foreach (AudioSource source in audioSources)
        {
            if (!source.loop) continue; // Only stop music (looping sounds)

            source.Stop();
            audioSourcesToDestroy.Add(source);
            Debug.Log("Stopped sound: " + source.clip.name);
        }
    }

    public void PauseSound(AudioClip clip)
    {
        foreach (AudioSource source in audioSources)
        {
            if (source.clip == clip)
            {
                source.Pause();
                Debug.Log("Paused sound: " + clip.name);
            }
        }
    }

    public void FadeOutSound(AudioClip clip, float duration)
    {
        foreach (AudioSource source in audioSources)
        {
            if (source.clip == clip)
            {
                GameManager.Instance.StartCoroutine(FadeOutCoroutine(source, duration));
            }
        }
    }

    private System.Collections.IEnumerator FadeOutCoroutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;

        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0, time / duration);
            yield return null;
        }

        source.Stop();
        audioSourcesToDestroy.Add(source);
    }

    public void ResumeSound(AudioClip clip)
    {
        foreach (AudioSource source in audioSources)
        {
            if (source.clip == clip)
            {
                source.UnPause();
            }
        }
    }


    public void Update()
    {
        foreach (AudioSource source in audioSources)
        {
            if (!source.isPlaying)
            {
                Debug.Log(source.clip.name + " finished playing, destroying source.");
                audioSourcesToDestroy.Add(source);
            }
        }

        for (int i = audioSourcesToDestroy.Count - 1; i >= 0; i--)
        {
            AudioSource source = audioSourcesToDestroy[i];
            audioSources.Remove(source);
            Destroy(source.gameObject);
        }
        audioSourcesToDestroy.Clear();
    }

}

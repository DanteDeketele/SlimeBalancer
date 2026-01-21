using System.Collections.Generic;
using UnityEngine;

public class SoundManager : BaseManager
{
    public AudioClip mainTheme;

    public AudioClip TiltGameScoreSound;

    public AudioClip GameOverSound;

    public AudioClip GameSelectSound;

    private List<AudioSource> audioSources = new List<AudioSource>();
    private List<AudioSource> audioSourcesToDestroy = new List<AudioSource>();

    void Awake()
    {

        
    }

    public void PlaySound(AudioClip clip, bool loop = false)
    {
        GameObject audioSourceObject = new GameObject("Soundtrack_" + clip.name);
        audioSourceObject.transform.parent = this.transform;
        AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
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
            }
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

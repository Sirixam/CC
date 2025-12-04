using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private int _sfxPoolSize = 10;

    private AudioSource _musicSource;
    private List<AudioSource> _sfxSources;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitSources();
    }

    // ------------------- PUBLIC METHODS ---------------------

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogError("Trying to play a null music clip");
            return;
        }

        _musicSource.clip = clip;
        _musicSource.volume = volume;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }

    public AudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null)
        {
            Debug.LogError("Trying to play a null SFX clip");
            return null;
        }

        AudioSource source = GetBestSfxSource();
        source.pitch = pitch;
        source.volume = volume;
        source.clip = clip;
        source.loop = loop;
        source.Play();
        return source;
    }

    // ------------------- PRIVATE  METHODS ---------------------

    private void InitSources()
    {
        // Music
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;

        // SFX
        _sfxSources = new List<AudioSource>(_sfxPoolSize);
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxSources.Add(source);
        }
    }

    private AudioSource GetBestSfxSource()
    {
        // Try use an available source
        foreach (var source in _sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        return FindLeastRemainingTimeSource();
    }

    private AudioSource FindLeastRemainingTimeSource()
    {
        AudioSource best = null;
        float minRemainingTime = float.MaxValue;

        foreach (var source in _sfxSources)
        {
            if (source.clip == null) return source;

            float remainingTime = source.clip.length - source.time;
            if (remainingTime < minRemainingTime)
            {
                minRemainingTime = remainingTime;
                best = source;
            }
        }

        return best;
    }
}

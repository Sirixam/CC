using System;
using UnityEngine;

public class GameAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition GameEnd;
        public AudioDefinition RoundEnd;
        public AudioClip BackgroundMusic;
        [Range(0f, 1f)] public float MusicVolume = 0.5f;
    }

    private Data _data;

    public GameAudioHelper(Data data)
    {
        _data = data;
    }

    //distraction
    public void OnGameEnd()
    {
        _data.GameEnd.Play();
    }

    public void OnRoundEnd()
    {
        _data.RoundEnd.Play();
    }

    public void PlayMusic()
    {
        if (_data.BackgroundMusic != null)
            AudioManager.Instance.PlayMusic(_data.BackgroundMusic, _data.MusicVolume);
    }

    public void StopMusic()
    {
        AudioManager.Instance.StopMusic();
    }

}
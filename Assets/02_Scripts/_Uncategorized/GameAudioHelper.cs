using System;
using UnityEngine;

public class GameAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition GameEnd;
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
}
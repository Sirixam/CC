using System;
using UnityEngine;

public class GameAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition GameEnd;
        public AudioDefinition PhaseChangeThink;
        public AudioDefinition PhaseChangeAnswer;
        public AudioDefinition PhaseChangeCheat;
        public AudioDefinition BeepFinal;
        public AudioDefinition BeepNotFinal;
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

    public void OnPhaseChangeThink()
    {
        _data.PhaseChangeThink.Play();
    }

    public void OnPhaseChangeAnswer()
    {
        _data.PhaseChangeAnswer.Play();
    }

    public void OnPhaseChangeCheat()
    {
        _data.PhaseChangeCheat.Play();
    }

    public void BeepFinal()
    {
        _data.BeepFinal.Play();
    }
    public void BeepNotFinal()
    {
        _data.BeepNotFinal.Play();
    }

}
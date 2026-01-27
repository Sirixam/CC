using System;
using UnityEngine;

public class PlayerAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition AnsweringAudio;
        public AudioDefinition AnsweringCorrectAudio;
        public AudioDefinition DashingAudio;
        public AudioDefinition PeekingAudio;
        public AudioDefinition SittingAudio;
        public AudioDefinition CollectingAudio;
        public AudioDefinition CheatingAudio;
        public AudioDefinition StunnedAudio;
    }

    private Data _data;
    private AudioSource _answeringAudioSource;
    private AudioSource _peekingAudioSource;

    public PlayerAudioHelper(Data data)
    {
        _data = data;
    }

    //answering
    public void OnStartAnswering()
    {
        _answeringAudioSource = _data.AnsweringAudio.Play();
    }

    public void TryStopAnswering()
    {
        _answeringAudioSource?.Stop();
    }

    //correctAnswer
    public void OnFinishedCorrectAnswer()
    {
        _data.AnsweringCorrectAudio.Play();
    }

    //dash
    public void OnStartDash()
    {
        _data.DashingAudio.Play();
    }

    //peek
    public void OnStartPeeking()
    {
        _peekingAudioSource = _data.PeekingAudio.Play();
    }

    public void OnStopPeeking()
    {
        _peekingAudioSource?.Stop();
    }

    //sit
    public void OnStartSitting()
    {
        _data.SittingAudio.Play();
    }

    //collect
    public void OnPickUp()
    {
        _data.CollectingAudio.Play();
    }

    //cheat
    public void OnStartCheating()
    {
        _data.CheatingAudio.Play();
    }

    public void OnStun()
    {
        _data.StunnedAudio.Play();
    }
}
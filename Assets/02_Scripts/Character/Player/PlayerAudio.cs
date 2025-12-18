using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioDefinition _answeringAudio;
    [SerializeField] private AudioDefinition _answeringCorrectAudio;
    [SerializeField] private AudioDefinition _dashingAudio;
    [SerializeField] private AudioDefinition _peekingAudio;
    [SerializeField] private AudioDefinition _sittingAudio;
    [SerializeField] private AudioDefinition _collectingAudio;
    [SerializeField] private AudioDefinition _cheatingAudio;
    [SerializeField] private AudioDefinition _throwingHitAudio;

    private AudioSource _answeringAudioSource;
    private AudioSource _peekingAudioSource;

    //answering
    public void OnStartAnswering()
    {
        _answeringAudioSource = _answeringAudio.Play();
    }

    public void TryStopAnswering()
    {
        _answeringAudioSource?.Stop();
    }

    //correctAnswer
    public void OnFinishedCorrectAnswer()
    {
        _answeringCorrectAudio.Play();
    }

    //dash
    public void OnStartDash()
    {
        _dashingAudio.Play();
    }

    //peek
    public void OnStartPeeking()
    {
        _peekingAudioSource = _peekingAudio.Play();
    }

    public void OnStopPeeking()
    {
        _peekingAudioSource?.Stop();
    }

    //sit
    public void OnStartSitting()
    {
        _sittingAudio.Play();
    }

    //collect
    public void OnPickUp()
    {
        _collectingAudio.Play();
    }

    //cheat
    public void OnStartCheating()
    {
        _cheatingAudio.Play();
    }

    //hit
    public void StartThrowHit()
    {
        _throwingHitAudio.Play();
    }
}
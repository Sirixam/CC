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

    private AudioSource _answeringAudioSource;
    private AudioSource _answeringCorrectAudioSource;
    private AudioSource _dashingAudioSource;
    private AudioSource _peekingAudioSource;
    private AudioSource _sittingAudioSource;
    private AudioSource _collectingAudioSource;
    private AudioSource _cheatingAudioSource;

    //answering
    public void StartAnswering()
    {
        _answeringAudioSource = _answeringAudio.Play();
    }

    public void TryStopAnswering()
    {
        _answeringAudioSource?.Stop();
    }

    //correctAnswer
    public void StartAnswerCorrect()
    {
        _answeringCorrectAudioSource = _answeringCorrectAudio.Play();
    }

    //dash
    public void StartDash()
    {
        _dashingAudioSource = _dashingAudio.Play();
    }

    //peek
    public void StartPeeking()
    {
        _peekingAudioSource = _peekingAudio.Play();
    }

    public void TryStopPeeking()
    {
        _peekingAudioSource?.Stop();
    }

    //sit
    public void StartSitting()
    {
        _sittingAudioSource = _sittingAudio.Play();
    }

    //collect
    public void StartCollecting()
    {
        _collectingAudioSource = _collectingAudio.Play();
    }

    //cheat
    public void StartCheatingLaugh()
    {
        _cheatingAudioSource = _cheatingAudio.Play();
    }






}
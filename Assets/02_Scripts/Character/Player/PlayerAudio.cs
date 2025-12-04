using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioDefinition _answeringAudio;

    private AudioSource _answeringAudioSource;

    public void StartAnswering()
    {
        _answeringAudioSource = _answeringAudio.Play();
    }

    public void TryStopAnswering()
    {
        _answeringAudioSource?.Stop();
    }
}

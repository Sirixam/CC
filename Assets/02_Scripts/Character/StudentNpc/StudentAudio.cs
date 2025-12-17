using UnityEngine;

public class StudentAudio : MonoBehaviour
{
    [SerializeField] private AudioDefinition _grumblingSoftAudio;
    [SerializeField] private AudioDefinition _grumblingMildAudio;
    [SerializeField] private AudioDefinition _grumblingStrongAudio;

    private AudioSource _grumblingSoftAudioSource;
    private AudioSource _grumblingMildAudioSource;
    private AudioSource _grumblingStrongAudioSource;

    //answering
    public void StartGrumblingSoft()
    {
        _grumblingSoftAudioSource = _grumblingSoftAudio.Play();
    }

    public void StartGrumblingMild()
    {
        _grumblingMildAudioSource = _grumblingMildAudio.Play();
    }

    public void StartGrumblingStrong()
    {
        _grumblingStrongAudioSource = _grumblingStrongAudio.Play();
    }


}
using UnityEngine;

public class ItemAudio : MonoBehaviour
{
    [SerializeField] private AudioDefinition _throwingHitAudio;

    private AudioSource _throwingHitAudioSource;

    //hit any surface
    public void OnCollide()
    {
        _throwingHitAudioSource = _throwingHitAudio.Play();
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "DEF_AudioClip_", menuName = "Definitions/Audio Clip")]
public class AudioDefinition : ScriptableObject
{
    [SerializeField] private AudioClip[] _clips;
    [SerializeField] private float _volume = 1;
    [SerializeField] private float _pitch = 1;
    [SerializeField] private bool _loop = false;

    public AudioSource Play()
    {
        if (_clips.Length == 0)
        {
            Debug.LogError("There's no audio clip assigned on: " + name);
            return null;
        }

        AudioClip randomClip = _clips[Random.Range(0, _clips.Length)];
        return AudioManager.Instance.PlaySFX(randomClip, _volume, _pitch, _loop);
    }
}

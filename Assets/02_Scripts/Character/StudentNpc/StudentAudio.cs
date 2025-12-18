using UnityEngine;

public class StudentAudio : MonoBehaviour
{
    [SerializeField] private AudioDefinition[] _grumblingAudiosByLevel;

    //distraction
    public void OnDistracted(int level)
    {
        int audioIndex = Mathf.Clamp(level, 0, _grumblingAudiosByLevel.Length - 1);
        _grumblingAudiosByLevel[audioIndex].Play();
    }
}
using System;
using UnityEngine;

public class StudentAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition[] GrumblingAudiosByLevel;
    }

    private Data _data;

    public StudentAudioHelper(Data data)
    {
        _data = data;
    }

    //distraction
    public void OnDistracted(int level)
    {
        int audioIndex = Mathf.Clamp(level, 0, _data.GrumblingAudiosByLevel.Length - 1);
        _data.GrumblingAudiosByLevel[audioIndex].Play();
    }
}
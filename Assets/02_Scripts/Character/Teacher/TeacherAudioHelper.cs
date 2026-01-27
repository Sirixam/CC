using System;

public class TeacherAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition CaughtAudio;
    }

    private Data _data;

    public TeacherAudioHelper(Data data)
    {
        _data = data;
    }

    public void OnGettingCaught()
    {
        _data.CaughtAudio.Play();
    }

}
using System;

public class TeacherAudioHelper
{
    [Serializable]
    public class Data
    {
        // Empty for now
    }

    private Data _data;

    public TeacherAudioHelper(Data data)
    {
        _data = data;
    }
}
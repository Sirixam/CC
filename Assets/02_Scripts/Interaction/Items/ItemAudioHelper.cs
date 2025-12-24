using System;
using UnityEngine;

public class ItemAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition CollisionAudio;
    }

    [SerializeField] private AudioDefinition _throwingHitAudio;

    private Data _data;

    public ItemAudioHelper(Data data)
    {
        _data = data;
    }

    //hit any surface
    public void OnCollide()
    {
        _data.CollisionAudio.Play();
    }
}
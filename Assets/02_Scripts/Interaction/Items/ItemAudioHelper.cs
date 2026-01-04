using System;
using UnityEngine;

public class ItemAudioHelper
{
    [Serializable]
    public class Data
    {
        public AudioDefinition CollisionAudio;
        public float MinVelocityOnCollide;
        public float CollisionCooldown;
    }

    [SerializeField] private AudioDefinition _throwingHitAudio;

    private Data _data;
    private float _nextPlayTime;

    public ItemAudioHelper(Data data)
    {
        _data = data;
    }

    //hit any surface
    public void OnCollide(Collision collision)
    {
        Debug.Log("Time : " + Time.time + " && nextPlayTime: " + _nextPlayTime);
        if (Time.time <= _nextPlayTime)
        {
            Debug.Log("CD is ON: " + _nextPlayTime);
            return;
        }

        float hitMagnitude = collision.relativeVelocity.magnitude;

        Debug.Log("hitMagnitude : " + hitMagnitude + " && minVelocity: " + _data.MinVelocityOnCollide);
        if (hitMagnitude <= _data.MinVelocityOnCollide)
        {
            Debug.Log("HitMagnitude: " + hitMagnitude);
            return;
        }

        _nextPlayTime = Time.time + _data.CollisionCooldown;
        Debug.Log("CD is : " + _nextPlayTime);
        //_source.pitch = Random.Range(0.9f, 1.1f);
        //float volume = Mathf.Clamp01(hitMagnitude / 10f);

        _data.CollisionAudio.Play();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeacherView : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _angryVFXs;

    public void PlayAngryVFX()
    {
        foreach (var vfx in _angryVFXs)
        {
            vfx.Play();
        }
    }

    public void StopAngryVFX()
    {
        foreach (var vfx in _angryVFXs)
        {
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}

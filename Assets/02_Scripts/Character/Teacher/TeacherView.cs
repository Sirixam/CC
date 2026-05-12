using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeacherView : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _angryVFXs;
    [SerializeField] private GameObject _newspaper;

    private void Awake()
    {
        if (_newspaper != null)
            _newspaper.SetActive(false);
    }

    public void ShowNewspaper()
    {
        if (_newspaper != null)
            _newspaper.SetActive(true);
    }

    public void HideNewspaper()
    {
        if (_newspaper != null)
            _newspaper.SetActive(false);
    }

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

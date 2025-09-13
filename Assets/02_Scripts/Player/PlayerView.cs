using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private ParticleSystem _stunVFX;

    private void Awake()
    {
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void OnStartStun()
    {
        _stunVFX.Play(withChildren: true);
    }

    public void OnStopStun()
    {
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
    }
}

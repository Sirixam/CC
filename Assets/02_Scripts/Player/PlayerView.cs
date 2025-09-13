using UnityEngine;
using PrimeTween;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private Transform _rendererContainer;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private ParticleSystem _stunVFX;
    [SerializeField] private TweenSettings<float> _startStunTweenSettings = new();
    [SerializeField] private TweenSettings<float> _stopStunTweenSettings = new();
    [SerializeField] private TweenSettings<Vector3> _pickUpTweenSettings = new();

    private MeshRenderer[] _meshRenderes;
    private float _initialBoundsExtentsZ;
    private bool _isSoftStunned;

    private void Awake()
    {
        _startStunTweenSettings.startFromCurrent = true;
        _stopStunTweenSettings.startFromCurrent = true;
        _pickUpTweenSettings.startFromCurrent = true;
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _meshRenderes = _rendererContainer.GetComponentsInChildren<MeshRenderer>();
        _initialBoundsExtentsZ = GetBounds().extents.z;
    }

    public void OnStartStun(bool isSoftStun)
    {
        _isSoftStunned = isSoftStun;
        _stunVFX.Play(withChildren: true);
        if (!isSoftStun)
        {
            Tween.ScaleZ(_rendererContainer, _startStunTweenSettings).OnUpdate(_rendererContainer, OnUpdateScaleZ);
        }
    }

    public void OnStopStun()
    {
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        if (!_isSoftStunned)
        {
            Tween.ScaleZ(_rendererContainer, _stopStunTweenSettings).OnUpdate(_rendererContainer, OnUpdateScaleZ);
        }
    }

    private void OnUpdateScaleZ(Transform target, Tween tween)
    {
        float zOffset = (1f - _rendererContainer.localScale.z) * _initialBoundsExtentsZ;
        _rendererContainer.localPosition = new Vector3(0f, 0f, zOffset);
    }

    private Bounds GetBounds()
    {
        Bounds bounds = new();
        foreach (var meshRenderer in _meshRenderes)
        {
            bounds.Encapsulate(meshRenderer.bounds);
        }
        return bounds;
    }

    public void OnPickUp(Transform item)
    {
        item.SetParent(_itemContainer, worldPositionStays: true);
        Tween.LocalPosition(item, _pickUpTweenSettings);
    }
}

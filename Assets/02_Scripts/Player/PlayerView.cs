using UnityEngine;
using PrimeTween;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private Transform _rendererContainer;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private ParticleSystem _stunVFX;
    [SerializeField] private TrailRenderer[] _dashTrails;
    [SerializeField] private TweenSettings<float> _startStunTweenSettings = new();
    [SerializeField] private TweenSettings<float> _stopStunTweenSettings = new();
    [SerializeField] private TweenSettings<Vector3> _pickUpTweenSettings = new();

    private MeshRenderer[] _meshRenderes;
    private float _initialBoundsExtentsZ;
    private bool _isSoftStunned;
    private Tween _scaleTweenZ;
    private Tween _positionTween;

    private void Awake()
    {
        _startStunTweenSettings.startFromCurrent = true;
        _stopStunTweenSettings.startFromCurrent = true;
        _pickUpTweenSettings.startFromCurrent = true;
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _meshRenderes = _rendererContainer.GetComponentsInChildren<MeshRenderer>();
        _initialBoundsExtentsZ = GetBounds().extents.z;

        foreach (var trailRenderer in _dashTrails)
        {
            trailRenderer.emitting = false;
        }
    }

    public void OnStartStun(bool isSoftStun)
    {
        _isSoftStunned = isSoftStun;
        _stunVFX.Play(withChildren: true);
        if (!isSoftStun)
        {
            _scaleTweenZ.Stop();
            _scaleTweenZ = Tween.ScaleZ(_rendererContainer, _startStunTweenSettings).OnUpdate(_rendererContainer, OnUpdateScaleZ);
        }
    }

    public void OnStopStun()
    {
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        if (!_isSoftStunned)
        {
            _scaleTweenZ.Stop();
            _scaleTweenZ = Tween.ScaleZ(_rendererContainer, _stopStunTweenSettings).OnUpdate(_rendererContainer, OnUpdateScaleZ);
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
        _positionTween.Stop();
        _positionTween = Tween.LocalPosition(item, _pickUpTweenSettings);
    }

    public void OnDrop(Transform item)
    {
        item.SetParent(null, worldPositionStays: true);
        _positionTween.Stop();
    }

    public void OnThrow(Transform item)
    {
        item.SetParent(null, worldPositionStays: true);
        _positionTween.Stop();
    }

    public void OnStartDash()
    {
        foreach (var trailRenderer in _dashTrails)
        {
            trailRenderer.emitting = true;
        }
    }

    public void OnStopDash()
    {
        foreach (var trailRenderer in _dashTrails)
        {
            trailRenderer.emitting = false;
        }
    }
}

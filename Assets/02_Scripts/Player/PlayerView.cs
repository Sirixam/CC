using UnityEngine;
using PrimeTween;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private Transform _rendererContainer;
    [SerializeField] private Transform _bodyRenderer;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private ParticleSystem _stunVFX;
    [SerializeField] private TrailRenderer[] _dashTrails;
    [SerializeField] private CheatUI _cheatUI;
    [SerializeField] private MemoryUI _memoryUI;
    [SerializeField] private TweenSettings<float> _startStunTweenSettings = new();
    [SerializeField] private TweenSettings<float> _stopStunTweenSettings = new();
    [SerializeField] private TweenSettings<float> _sittingTweenSettings = new();
    [SerializeField] private TweenSettings<float> _standingTweenSettings = new();
    [SerializeField] private TweenSettings<Vector3> _pickUpTweenSettings = new();

    private MeshRenderer[] _meshRenderes;
    private float _initialBoundsExtentsY;
    private float _initialBoundsExtentsZ;
    private bool _isSoftStunned;
    private Tween _scaleTweenZ;
    private Tween _scaleTweenY;
    private Tween _positionTween;

    public Vector3 PickUpPosition => _itemContainer.position + _pickUpTweenSettings.endValue;
    public CheatUI CheatUI => _cheatUI;
    public MemoryUI MemoryUI => _memoryUI;

    private void Awake()
    {
        _cheatUI.Hide();
        _memoryUI.Hide();
        _startStunTweenSettings.startFromCurrent = true;
        _stopStunTweenSettings.startFromCurrent = true;
        _sittingTweenSettings.startFromCurrent = true;
        _standingTweenSettings.startFromCurrent = true;
        _pickUpTweenSettings.startFromCurrent = true;
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _meshRenderes = _rendererContainer.GetComponentsInChildren<MeshRenderer>();
        _initialBoundsExtentsY = GetBounds().extents.y;
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

    private void OnUpdateScaleY(Transform target, Tween tween)
    {
        float yOffset = (1f - target.localScale.y) * -_initialBoundsExtentsY;
        _rendererContainer.localPosition = new Vector3(_rendererContainer.localPosition.x, yOffset, _rendererContainer.localPosition.z);
    }

    private void OnUpdateScaleZ(Transform target, Tween tween)
    {
        float zOffset = (1f - target.localScale.z) * _initialBoundsExtentsZ;
        _rendererContainer.localPosition = new Vector3(_rendererContainer.localPosition.x, _rendererContainer.localPosition.y, zOffset);
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

    public void OnSitting()
    {
        _scaleTweenY.Stop();
        _scaleTweenY = Tween.ScaleY(_bodyRenderer, _sittingTweenSettings).OnUpdate(_bodyRenderer, OnUpdateScaleY);
    }

    public void OnStanding()
    {
        _scaleTweenY.Stop();
        _scaleTweenY = Tween.ScaleY(_bodyRenderer, _standingTweenSettings).OnUpdate(_bodyRenderer, OnUpdateScaleY);
    }
}

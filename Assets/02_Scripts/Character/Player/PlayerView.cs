using System;
using UnityEngine;
using PrimeTween;

public class PlayerView : MonoBehaviour, IStunView, IChairView
{
    [SerializeField] private Transform _rendererContainer;
    [SerializeField] private Transform _bodyRenderer;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private ParticleSystem _stunVFX;
    [SerializeField] private TrailRenderer[] _dashTrails;
    [SerializeField] private CheatUI _peekUI;
    [SerializeField] private CheatUI _cheatUI;
    [SerializeField] private CraftingUI _craftingUI;
    [SerializeField] private MemoryUI _memoryUI;
    [SerializeField] private ThrowPreviewComponent _throwPreview;
    [SerializeField] private float _caughtFreezeDuration = 0.2f;
    [SerializeField] private float _caughtShakeAmount = 0.3f;
    [SerializeField] private float _caughtShakeDuration = 0.07f;
    [SerializeField] private int _caughtShakeCount = 5;
    [SerializeField] private ParticleSystem _caughtCloudVFXPrefab;
    [SerializeField] private ParticleSystem _caughtSymbolsVFX;
    [SerializeField] private HandAnimator _handAnimator;
    [SerializeField] private LobThrowPreviewComponent _lobThrowPreview;

    public void ShowLobThrowPreview() => _lobThrowPreview.Show();
    public void HideLobThrowPreview() => _lobThrowPreview.Hide();

    private bool _isAnswerSheetLifted;

    private Sequence _caughtSequence;
    private Tween _caughtShrinkTween;

    [SerializeField] private TweenSettings<float> _startStunTweenSettings = new();
    [SerializeField] private TweenSettings<float> _stopStunTweenSettings = new();
    [SerializeField] private TweenSettings<float> _sittingTweenSettings = new();
    [SerializeField] private TweenSettings<float> _standingTweenSettings = new();
    [SerializeField] private TweenSettings<Vector3> _pickUpTweenSettings = new();

    private MeshRenderer[] _meshRenderers;
    private float _initialBoundsExtentsY;
    private float _initialBoundsExtentsZ;
    private bool _isSoftStunned;
    private Tween _scaleTweenZ;
    private Tween _scaleTweenY;
    private Tween _positionTween;
    private Sequence _heldItemShakeTween;


    public Vector3 PickUpPosition => _itemContainer.position + _pickUpTweenSettings.endValue;
    public CheatUI PeekUI => _peekUI;
    public CheatUI CheatUI => _cheatUI;
    public CraftingUI CraftingUI => _craftingUI;
    public MemoryUI MemoryUI => _memoryUI;

    private void Awake()
    {
        _peekUI.Hide();
        _cheatUI.Hide();
        _craftingUI.Hide();
        _memoryUI.Hide();
        _startStunTweenSettings.startFromCurrent = true;
        _stopStunTweenSettings.startFromCurrent = true;
        _sittingTweenSettings.startFromCurrent = true;
        _standingTweenSettings.startFromCurrent = true;
        _pickUpTweenSettings.startFromCurrent = true;
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _meshRenderers = _rendererContainer.GetComponentsInChildren<MeshRenderer>();
        _initialBoundsExtentsY = GetBounds().extents.y;
        _initialBoundsExtentsZ = GetBounds().extents.z;

        foreach (var trailRenderer in _dashTrails)
        {
            trailRenderer.emitting = false;
        }
    }

    public void Inject(IAnswerIconProvider answerIconProvider)
    {
        _memoryUI.Inject(answerIconProvider);
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
        foreach (var meshRenderer in _meshRenderers)
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

    public void ShowThrowPreview()
    {
        _throwPreview.Show();
    }

    public void HideThrowPreview()
    {
        _throwPreview.Hide();
    }

    public void OnThrow(Transform item)
    {
        _throwPreview.Hide();
        _lobThrowPreview.Hide();
        item.SetParent(null, worldPositionStays: true);
        _positionTween.Stop();
    }

    public void OnStartForce()
    {
        foreach (var trailRenderer in _dashTrails)
        {
            trailRenderer.emitting = true;
        }
    }

    public void OnStopForce()
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

    public void OnCaught(Vector3 worldPosition, Action onComplete)
    {
        _caughtSequence.Stop();
        _caughtShrinkTween.Stop();

        // Calculate total shake duration so shrink matches it exactly
        float totalShakeDuration = (_caughtShakeCount + 1) * _caughtShakeDuration;

        _caughtShrinkTween = Tween.Scale(_rendererContainer, Vector3.zero, totalShakeDuration, Ease.InBack, startDelay: _caughtFreezeDuration);

        _caughtSequence = Sequence.Create().ChainDelay(_caughtFreezeDuration);

        for (int i = 0; i < _caughtShakeCount; i++)
        {
            float dir = (i % 2 == 0) ? _caughtShakeAmount : -_caughtShakeAmount;
            _caughtSequence.Chain(
                Tween.LocalPositionX(_rendererContainer, dir, _caughtShakeDuration, Ease.OutQuad)
            );
        }

        _caughtSequence.Chain(Tween.LocalPositionX(_rendererContainer, 0f, _caughtShakeDuration, Ease.OutQuad))
            .OnComplete(() =>
            {
                _caughtShrinkTween.Stop();
                _rendererContainer.localScale = Vector3.one;

                if (_caughtCloudVFXPrefab != null)
                {
                    ParticleSystem cloud = UnityEngine.Object.Instantiate(
                        _caughtCloudVFXPrefab, worldPosition, Quaternion.identity
                    );
                    UnityEngine.Object.Destroy(cloud.gameObject,
                        cloud.main.duration + cloud.main.startLifetime.constantMax);
                }

                onComplete?.Invoke();
            });
    }

    public void StartWriting()
    {
        _handAnimator.SetWriting();
    }

    public void StopWriting()
    {
        if (_isAnswerSheetLifted)
        {
            _handAnimator.SetValidating();
        }
        else
        {
            _handAnimator.SetHidden();
        }
    }

    public void OnLiftAnswerSheet()
    {
        _isAnswerSheetLifted = true;
        _handAnimator.SetValidating();
    }

    public void OnLowerAnswerSheet()
    {
        _isAnswerSheetLifted = false;
        _handAnimator.SetHidden();
    }

    public void StartCrafting()
    {
        _handAnimator.SetCrafting();
    }

    public void StopCrafting()
    {
        _handAnimator.SetHidden();
    }

    public void ResetVisuals()
    {
        _stunVFX.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _rendererContainer.localScale = Vector3.one;
        _rendererContainer.localPosition = Vector3.zero;
        _scaleTweenZ.Stop();
        _scaleTweenY.Stop();
        _caughtSequence.Stop();
        _caughtShrinkTween.Stop();
        OnLowerAnswerSheet();

        foreach (var trail in _dashTrails)
        {
            trail.emitting = false;
        }

        if (_caughtSymbolsVFX != null)
        {
            _caughtSymbolsVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    public void PlayCaughtSymbols()
    {
        if (_caughtSymbolsVFX != null)
        {
            _caughtSymbolsVFX.Play();
        }
    }

    public void StopCaughtSymbols()
    {
        if (_caughtSymbolsVFX != null)
        {
            _caughtSymbolsVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void ShakeHeldItem()
    {
        if (_itemContainer.childCount == 0) return;
        Transform item = _itemContainer.GetChild(0);

        _heldItemShakeTween.Stop();
        _heldItemShakeTween = Sequence.Create(cycles: -1)
            .Chain(Tween.LocalPositionX(item, 0.05f, 0.05f, Ease.OutQuad))
            .Chain(Tween.LocalPositionX(item, -0.05f, 0.05f, Ease.OutQuad));
    }

    public void StopShakeHeldItem()
    {
        _heldItemShakeTween.Stop();
        if (_itemContainer.childCount > 0)
        {
            _itemContainer.GetChild(0).localPosition = Vector3.zero;
        }
    }

    public void InitializeThrowPreview(ChairHelper chairHelper, ThrowHelper.Data throwData, int flyingLayer)
    {
        _throwPreview.Initialize(chairHelper, throwData, flyingLayer);
    }
    public void InitializeLobThrowPreview(ChairHelper chairHelper, LobThrowHelper lobThrowHelper, int flyingLayer)
    {
        _lobThrowPreview.Initialize(chairHelper, lobThrowHelper, flyingLayer);
    }
    
}

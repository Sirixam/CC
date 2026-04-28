using System;
using System.Collections;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class LightbulbUI : MonoBehaviour
{
    private enum EState
    {
        Undefined,
        On,
        Off
    }

    [SerializeField] private GameObject _offState;
    [SerializeField] private GameObject _onState;
    [SerializeField] private EState _initialState;
    [SerializeField] private float _defaultHideDelay = 5f;

    [Header("Features Flags")]
    [SerializeField] private bool _useOffState;

    [Header("Shine")]
    [SerializeField] private Image _shineOverlay;
    [SerializeField] private float _shineDuration = 0.35f;

    [Header("Float")]
    [SerializeField] private float _floatAmplitude = 8f;
    [SerializeField] private float _floatDuration = 0.7f;

    private EState _state;
    private Coroutine _autoHideCoroutine;
    private Sequence _shineTween;
    private Tween _floatTween;
    private RectTransform _rect;
    private Vector2 _restAnchoredPosition;

    public bool IsShown => gameObject.activeSelf;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _restAnchoredPosition = _rect.anchoredPosition;

        if (_shineOverlay != null)
            _shineOverlay.color = new Color(1f, 1f, 1f, 0f);

        if (_state == EState.Undefined)
        {
            SetState(_initialState == EState.On);
        }
    }

    public void HideDelayed()
    {
        CancelHideCoroutine();
        _autoHideCoroutine = StartCoroutine(WaitSecondsRoutine(_defaultHideDelay, Hide));
    }

    public void HideDelayed(float delay)
    {
        CancelHideCoroutine();
        _autoHideCoroutine = StartCoroutine(WaitSecondsRoutine(delay, Hide));
    }

    public void Show()
    {
        CancelHideCoroutine();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        CancelHideCoroutine();
        StopFloat();
        gameObject.SetActive(false);
    }

    public void SetState(bool isOn)
    {
        _state = isOn ? EState.On : EState.Off;
        _onState.SetActive(isOn);
        _offState.SetActive(!isOn && _useOffState);
    }

    public void PlayShine()
    {
        if (_shineOverlay == null) return;
        _shineTween.Stop();
        _shineOverlay.color = new Color(1f, 1f, 1f, 0f);
        _shineTween = Sequence.Create()
            .Chain(Tween.Alpha(_shineOverlay, startValue: 0f, endValue: 0.85f, duration: _shineDuration * 0.3f, Ease.OutQuad))
            .Chain(Tween.Alpha(_shineOverlay, startValue: 0.85f, endValue: 0f, duration: _shineDuration * 0.7f, Ease.InQuad));
    }

    public void StartFloat()
    {
        _floatTween.Stop();
        _rect.anchoredPosition = _restAnchoredPosition;
        _floatTween = Tween.UIAnchoredPositionY(_rect,
            startValue: _restAnchoredPosition.y - _floatAmplitude,
            endValue: _restAnchoredPosition.y + _floatAmplitude,
            duration: _floatDuration,
            ease: Ease.InOutSine,
            cycles: -1,
            cycleMode: CycleMode.Yoyo);
    }

    public void StopFloat()
    {
        _floatTween.Stop();
        if (_rect != null)
            _rect.anchoredPosition = _restAnchoredPosition;
    }

    private IEnumerator WaitSecondsRoutine(float seconds, Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback?.Invoke();
    }

    private void CancelHideCoroutine()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
        }
    }
}

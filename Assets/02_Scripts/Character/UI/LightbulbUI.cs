//#define DEPRECATED // COMMENT THIS TO START USING FEATURE AGAIN

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

    private void Awake()
    {
#if DEPRECATED
        gameObject.SetActive(false);
#else
        _rect = GetComponent<RectTransform>();
        _restAnchoredPosition = _rect.anchoredPosition;

        if (_shineOverlay != null)
            _shineOverlay.color = new Color(1f, 1f, 1f, 0f);

        if (_state == EState.Undefined)
        {
            SetState(_initialState == EState.On);
        }
#endif
    }

    public void HideDelayed()
    {
#if !DEPRECATED
        CancelHideCoroutine();
        _autoHideCoroutine = StartCoroutine(WaitSecondsRoutine(_defaultHideDelay, Hide));
#endif
    }

    public void HideDelayed(float delay)
    {
#if !DEPRECATED
        CancelHideCoroutine();
        _autoHideCoroutine = StartCoroutine(WaitSecondsRoutine(delay, Hide));
#endif
    }

    public void Show()
    {
#if !DEPRECATED
        CancelHideCoroutine();
        gameObject.SetActive(true);
#endif
    }

    public void Hide()
    {
#if !DEPRECATED
        CancelHideCoroutine();
        StopFloat();
        gameObject.SetActive(false);
#endif
    }

    public void SetState(bool isOn)
    {
#if !DEPRECATED
        _state = isOn ? EState.On : EState.Off;
        _onState.SetActive(isOn);
        _offState.SetActive(!isOn && _useOffState);
#endif
    }

    public void PlayShine()
    {
#if !DEPRECATED
        if (_shineOverlay == null) return;
        _shineTween.Stop();
        _shineOverlay.color = new Color(1f, 1f, 1f, 0f);
        _shineTween = Sequence.Create()
            .Chain(Tween.Alpha(_shineOverlay, startValue: 0f, endValue: 0.85f, duration: _shineDuration * 0.3f, Ease.OutQuad))
            .Chain(Tween.Alpha(_shineOverlay, startValue: 0.85f, endValue: 0f, duration: _shineDuration * 0.7f, Ease.InQuad));
#endif
    }

    public void StartFloat()
    {
#if !DEPRECATED
        _floatTween.Stop();
        _rect.anchoredPosition = _restAnchoredPosition;
        _floatTween = Tween.UIAnchoredPositionY(_rect,
            startValue: _restAnchoredPosition.y - _floatAmplitude,
            endValue: _restAnchoredPosition.y + _floatAmplitude,
            duration: _floatDuration,
            ease: Ease.InOutSine,
            cycles: -1,
            cycleMode: CycleMode.Yoyo);
#endif
    }

    public void StopFloat()
    {
#if !DEPRECATED
        _floatTween.Stop();
        if (_rect != null)
            _rect.anchoredPosition = _restAnchoredPosition;
#endif
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

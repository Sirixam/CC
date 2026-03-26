//#define DEPRECATED // COMMENT THIS TO START USING FEATURE AGAIN

using System;
using System.Collections;
using UnityEngine;

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

    private EState _state;
    private Coroutine _autoHideCoroutine;

    private void Awake()
    {
#if DEPRECATED
        gameObject.SetActive(false);
#else
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

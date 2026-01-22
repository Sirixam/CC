using PrimeTween;
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
        gameObject.SetActive(false);
    }

    public void SetState(bool isOn)
    {
        _state = isOn ? EState.On : EState.Off;
        _onState.SetActive(isOn);
        _offState.SetActive(!isOn && _useOffState);
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

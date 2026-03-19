using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class TimeHelper
{
    private TimeUI _timeUI;

    private bool _isPaused;
    public bool IsRunning { get; private set; }
    public float _remainingTime;

    public Action OnTimesUp;

    public TimeHelper(TimeUI timeUI)
    {
        _timeUI = timeUI;
    }

    public void Setup(float maxTimeInSeconds)
    {
        _remainingTime = maxTimeInSeconds;
        _timeUI.Setup(maxTimeInSeconds);
    }

    public async UniTask StartTimer(CancellationToken cancellationToken)
    {
        if (IsRunning) return;

        if (_timeUI != null)
        {
            _timeUI.gameObject.SetActive(true);
        }

        IsRunning = true;
        while (IsRunning && !cancellationToken.IsCancellationRequested)
        {
            await UniTask.Yield();
            if (_isPaused) continue;

            _remainingTime = Mathf.Max(0, _remainingTime - Time.deltaTime);
            _timeUI.SetRemainingTime(_remainingTime);

            if (_remainingTime <= 0)
            {
                IsRunning = false;
                OnTimesUp?.Invoke();
            }
        }
        IsRunning = false;
    }

    public void Pause()
    {
        _isPaused = true;
    }

    public void Resume()
    {
        _isPaused = false;
    }
    public void Stop()
    {
        IsRunning = false;
        _isPaused = false;
    }
}

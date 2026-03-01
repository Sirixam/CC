using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
//using System.Diagnostics;

public class RoundTimeHelper
{
    private RoundTimeUI _roundTimeUI;
    public float ElapsedTime { get; private set; }
    public float TotalDuration { get; private set; }

    public event Action<float> OnTimeUpdated;

    private int _currentMilestoneIndex;
    private bool _isPaused;
    public bool IsRunning { get; private set; }
    private float _roundRemainingTime;
    public Action OnRoundTimesUp;
    public TimeHelper _timeHelper;

    public RoundTimeHelper(RoundTimeUI roundTimeUI)
    {
        _roundTimeUI = roundTimeUI;
    }

    public void Setup(GlobalDefinition globalDef)
    {
        float p1 = (globalDef.PreAnsweringDelay.x + globalDef.PreAnsweringDelay.y) / 2f;
        float p2 = (globalDef.AnsweringDelay.x + globalDef.AnsweringDelay.y) / 2f;
        float p3 = (globalDef.PostAnsweringDelay.x + globalDef.PostAnsweringDelay.y) / 2f;
        _roundRemainingTime = p1 + p2 + p3;
        _roundTimeUI.Setup(_roundRemainingTime, globalDef);
    }

    public async UniTask StartTimer(CancellationToken cancellationToken)
    {
        //Debug.Log("Is Running? " + IsRunning);
        if (IsRunning) return;
       
        if (_roundTimeUI != null)
        {
            _roundTimeUI.gameObject.SetActive(true);
        }

        IsRunning = true;
        while (IsRunning && !cancellationToken.IsCancellationRequested)
        {
            await UniTask.Yield();
            if (_isPaused) continue;

            _roundRemainingTime = Mathf.Max(0, _roundRemainingTime - Time.deltaTime);
            _roundTimeUI.SetRoundRemainingTime(_roundRemainingTime);
            //Debug.Log("_roundRemainingTime? " + _roundRemainingTime);

            if (_roundRemainingTime <= 0)
            {
                IsRunning = false;
                OnRoundTimesUp?.Invoke();
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

}

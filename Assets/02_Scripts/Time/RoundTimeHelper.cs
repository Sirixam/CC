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
    public event Action<int> OnPhaseChanged;
    public event Action<int, int> OnCountdownBeep;
    public event Action OnLoopRestarted;



    private int _currentMilestoneIndex;
    private bool _isPaused;
    public bool IsRunning { get; private set; }
    private float _roundRemainingTime;
    public Action OnRoundTimesUp;
    public TimeHelper _timeHelper;

    private List<float> _phaseThresholds = new List<float>();
    private int _currentPhaseIndex;
    private const int BEEP_COUNTDOWN_SECONDS = 3;
    private int _lastBeepSecond = -1;

    //store original values so we can reset the loop
    private float _totalRoundDuration;
    private float _p1, _p2, _p3;

    // NEW: whether the timer should loop indefinitely
    public bool IsLooping { get; set; } = false;



    public RoundTimeHelper(RoundTimeUI roundTimeUI)
    {
        _roundTimeUI = roundTimeUI;
    }

    public void Setup(GlobalDefinition globalDef)
    {
        _p1 = (globalDef.PreAnsweringDelay.x + globalDef.PreAnsweringDelay.y) / 2f;
        _p2 = (globalDef.AnsweringDuration.x + globalDef.AnsweringDuration.y) / 2f;
        _p3 = (globalDef.PostAnsweringDelay.x + globalDef.PostAnsweringDelay.y) / 2f;
        _totalRoundDuration = _p1 + _p2 + _p3;

        _roundRemainingTime = _totalRoundDuration;

        _phaseThresholds.Clear();
        _phaseThresholds.Add(_p2 + _p3);

        _currentPhaseIndex = 0;
        _lastBeepSecond = -1;

        _roundTimeUI.Setup(_roundRemainingTime, globalDef);
    }

    // NEW: extracted so we can call it on loop restart too
    private void ResetCycle()
    {
        _roundRemainingTime = _totalRoundDuration; // 

        _phaseThresholds.Clear();
        _phaseThresholds.Add(_p2 + _p3);

        _currentPhaseIndex = 0;
        _lastBeepSecond = -1;

        _roundTimeUI.ResetFill(); // 
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

            //check if we've crossed the next phase threshold
            if (_currentPhaseIndex < _phaseThresholds.Count &&
                _roundRemainingTime <= _phaseThresholds[_currentPhaseIndex])
            {
                OnPhaseChanged?.Invoke(_currentPhaseIndex);
                _lastBeepSecond = -1; // reset beep tracker for new phase
                _currentPhaseIndex++;
            }

            // NEW: check countdown beeps
            CheckCountdownBeep();

            if (_roundRemainingTime <= 0)
            {
                OnRoundTimesUp?.Invoke();

                if (IsLooping)
                {
                    ResetCycle();
                    OnLoopRestarted?.Invoke();
                }
                else
                {
                    IsRunning = false;
                }
            }
        }
        IsRunning = false;
    }

    // calculates how many seconds remain in the CURRENT phase
    // and fires a beep event once per second during the countdown window
    private void CheckCountdownBeep()
    {
        float phaseEndTime = _currentPhaseIndex < _phaseThresholds.Count
            ? _phaseThresholds[_currentPhaseIndex]
            : 0f;

        float timeRemainingInPhase = _roundRemainingTime - phaseEndTime;

        if (timeRemainingInPhase <= BEEP_COUNTDOWN_SECONDS && timeRemainingInPhase > 0f)
        {
            int secondsLeft = Mathf.CeilToInt(timeRemainingInPhase);

            // Only fire once per second
            if (secondsLeft != _lastBeepSecond)
            {
                _lastBeepSecond = secondsLeft;
                OnCountdownBeep?.Invoke(_currentPhaseIndex, secondsLeft);
            }
        }
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

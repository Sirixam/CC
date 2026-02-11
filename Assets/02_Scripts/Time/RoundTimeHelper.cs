using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
//using System.Diagnostics;

public class RoundTimeHelper
{
    //[SerializeField] public List<RoundTimerMilestone> milestones;
    private RoundTimeUI _roundTimeUI;
    public float ElapsedTime { get; private set; }
    public float TotalDuration { get; private set; }

    public event Action<float> OnTimeUpdated;
    public event Action<int> OnMilestoneReached;

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
    /*
    void Awake()
    {
        TotalDuration = 0f;
        foreach (var m in milestones)
            TotalDuration += m.duration;
    } */

    public void Setup(float maxTimeInSeconds)
    {
        _roundRemainingTime = maxTimeInSeconds;
        _roundTimeUI.Setup(maxTimeInSeconds);
    }

    public async UniTask StartTimer(CancellationToken cancellationToken)
    {
        Debug.Log("Is Running? " + IsRunning);
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
            Debug.Log("_roundRemainingTime? " + _roundRemainingTime);

            if (_roundRemainingTime <= 0)
            {
                //Debug.Log("remaining Time time helper" + _timeHelper._remainingTime);
                /*
                Debug.Log("remaining Time time helper" + _timeHelper._remainingTime);
                if (_timeHelper._remainingTime <= 0)
                {
                    Debug.Log("remaining Time time helper" + _timeHelper._remainingTime);
                    _timeHelper.OnTimesUp.Invoke();
                } else
                */
                IsRunning = false;
                OnRoundTimesUp.Invoke();
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

    /*
    void Update()
    {
        if (!_running) return;

        if (_roundTimeUI != null)
        {
            _roundTimeUI.gameObject.SetActive(true);
        }

        ElapsedTime += Time.deltaTime;
        OnTimeUpdated?.Invoke(ElapsedTime);

        CheckMilestones();
    }

    void CheckMilestones()
    {
        float t = ElapsedTime;

        for (int i = 0; i < milestones.Count; i++)
        {
            if (t < milestones[i].duration)
                return;

            t -= milestones[i].duration;

            if (_currentMilestoneIndex == i)
            {
                milestones[i].onReached?.Invoke();
                OnMilestoneReached?.Invoke(i);
                _currentMilestoneIndex++;
            }
        }

        if (_currentMilestoneIndex >= milestones.Count)
            _running = false;
    } */
}

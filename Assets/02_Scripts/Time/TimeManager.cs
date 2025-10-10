using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private float _maxTimeInSeconds = 30;
    [SerializeField] private TimeUI _timeUI;
    [SerializeField] private GameObject _timesUpFeedback;

    private bool _isPaused;
    private bool _isStarted;
    private float _remainingTime;

    private void Awake()
    {
        _remainingTime = _maxTimeInSeconds;
        _timeUI.Setup(_maxTimeInSeconds);
        _timesUpFeedback.SetActive(false);
    }

    private void Update()
    {
        if (!_isStarted || _isPaused || _remainingTime <= 0) return;

        _remainingTime = Mathf.Max(0, _remainingTime - Time.deltaTime);
        _timeUI.SetRemainingTime(_remainingTime);

        if (_remainingTime <= 0)
        {
            _timesUpFeedback.SetActive(true);
        }
    }

    public void StartTimer()
    {
        _isStarted = true;
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

using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    [SerializeField] private Image _fill;

    private float _maxTimeInSeconds;

    public void Setup(float seconds)
    {
        _fill.fillAmount = 0;
        _maxTimeInSeconds = seconds;
    }

    public void SetRemainingTime(float seconds)
    {
        float percent = (_maxTimeInSeconds - seconds) / _maxTimeInSeconds;
        _fill.fillAmount = percent;
    }
}
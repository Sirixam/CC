using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
//using System.Diagnostics;
public class RoundTimeUI : MonoBehaviour
{
    [SerializeField] private Image _fill;

    private float _maxRoundTimeInSeconds;
    private float _totalVisualWeight;

    public void Setup(float seconds)
    {
        _fill.fillAmount = 0;
        _maxRoundTimeInSeconds = seconds;
    }

    public void SetRoundRemainingTime(float seconds)
    {
        float percent = Mathf.Clamp01((_maxRoundTimeInSeconds - seconds) / _maxRoundTimeInSeconds);
        _fill.fillAmount = percent;
    }

}

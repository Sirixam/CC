using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Drawing;
using System;
using Color = UnityEngine.Color;

public class TimeUI : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] public TextMeshProUGUI _timerText;
    [SerializeField] private Color thinkingColor;
    [SerializeField] private Color writingColor;
    [SerializeField] private Color cheatingColor;

    [SerializeField] private float writingThreshold = 0.5f;
    [SerializeField] private float cheatingThreshold = 0.65f;

    private float _maxTimeInSeconds;

    public void Setup(float seconds)
    {
        //_fill.fillAmount = 0;
        _maxTimeInSeconds = seconds;
    }

    public void SetRemainingTime(float seconds)
    {
        float percent = (_maxTimeInSeconds - seconds) / _maxTimeInSeconds;
        //_fill.fillAmount = percent;

        // Update the UI text and format to seconds
        _timerText.color = EvaluateColor(percent);
        //_timerText.text = seconds.ToString("000");
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        _timerText.text = time.ToString(@"mm\:ss");
    }

    private Color EvaluateColor(float percent)
    {
        if (percent < writingThreshold)
            return thinkingColor;

        if (percent < cheatingThreshold)
            return writingColor;

        return cheatingColor;
    }
}
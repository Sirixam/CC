using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Drawing;
using Color = UnityEngine.Color;
using TMPro;
//using System.Diagnostics;
public class RoundTimeUI : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] public TextMeshProUGUI _timerText;
    [SerializeField] private Color thinkingColor;
    [SerializeField] private Color writingColor;
    [SerializeField] private Color cheatingColor;

    [SerializeField] private float writingThreshold = 0.5f;
    [SerializeField] private float cheatingThreshold = 0.65f;

    private float _maxRoundTimeInSeconds; 
    private Vector2[] _ranges;
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

        // Update the UI text and format to seconds
        _timerText.color = EvaluateColor(percent);
        _timerText.text = seconds.ToString("00");
        
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Drawing;
using System;
using Color = UnityEngine.Color;

public class TimeUIBell : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI _timerText;

    private float _maxTimeInSeconds;

    public void Setup(float seconds)
    {
        _maxTimeInSeconds = seconds;
    }

    public void SetRemainingTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        _timerText.text = time.ToString(@"mm\:ss");
    }
}
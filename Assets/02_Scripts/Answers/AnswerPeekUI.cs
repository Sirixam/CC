using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class AnswerPeekUI : MonoBehaviour
{
    [SerializeField] private Image _playerIcon;
    [SerializeField] private Image _answerTypeIcon;
    [SerializeField] private RectTransform _readyObject;
    [SerializeField] private Image _progressMask;
    [SerializeField] private Image _progressFill;
    [Header("Configurations")]
    [SerializeField] private Gradient _progressGradient;
    [SerializeField] private TweenSettings<Vector2> _notReadyTweenSettings;
    [SerializeField] private TweenSettings<Vector2> _readyTweenSettings;

    private Tween _readyTween;

    private void Awake()
    {
        _notReadyTweenSettings.startFromCurrent = true;
        _readyTweenSettings.startFromCurrent = true;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Setup(Sprite playerIcon, Sprite answerTypeIcon, float progress)
    {
        _playerIcon.sprite = playerIcon;
        _answerTypeIcon.sprite = answerTypeIcon;
        _readyObject.anchoredPosition = progress >= 1 ? _readyTweenSettings.endValue : _notReadyTweenSettings.endValue;
        SetProgress(progress);
    }

    public void ShowNotReady()
    {
        _readyTween.Stop();
        _readyTween = Tween.UIAnchoredPosition(_readyObject, _notReadyTweenSettings);
    }

    public void ShowReady()
    {
        _readyTween.Stop();
        _readyTween = Tween.UIAnchoredPosition(_readyObject, _readyTweenSettings);
    }

    public void SetProgress(float percent)
    {
        _progressMask.fillAmount = percent;
        _progressFill.color = _progressGradient.Evaluate(percent);
    }
}

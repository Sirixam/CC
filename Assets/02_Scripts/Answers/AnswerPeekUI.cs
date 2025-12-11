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

    public AnswerPeek AnswerPeek { get; private set; }

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

    public void Setup(AnswerPeek answerPeek, Sprite playerIcon, Sprite answerTypeIcon)
    {
        AnswerPeek = answerPeek;
        _playerIcon.sprite = playerIcon;
        _answerTypeIcon.sprite = answerTypeIcon;
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        bool isFull = AnswerPeek.AnswerSheet.IsAnswerFull(AnswerPeek.AnswerID, out float progress);
        _readyObject.anchoredPosition = isFull ? _readyTweenSettings.endValue : _notReadyTweenSettings.endValue;
        if (!isFull)
        {
            SetProgress(progress);
        }
        else
        {
            SetProgress(AnswerPeek.ValidationPercent);
        }
    }

    public void Clear()
    {
        AnswerPeek = null;
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

    private void SetProgress(float percent)
    {
        _progressMask.fillAmount = percent;
        _progressFill.color = _progressGradient.Evaluate(percent);
    }
}

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
    [SerializeField] private Vector2 _notReadyPosition;
    [SerializeField] private TweenSettings<Vector2> _readyTweenSettings;

    private Tween _readyTween;
    private bool _isFull;

    public AnswerPeek AnswerPeek { get; private set; }

    private void Awake()
    {
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
        UpdateProgress(setup: true);
    }

    public void UpdateProgress(bool setup)
    {
        bool isFull = AnswerPeek.AnswerSheet.IsAnswerFull(AnswerPeek.AnswerID, out float progress);
        if (!isFull)
        {
            SetProgress(progress);
        }
        else
        {
            SetProgress(1 - AnswerPeek.AnswerController.ValidatingPercent); // Go backwards
        }
        if (isFull != _isFull || setup)
        {
            if (setup)
            {
                _readyObject.anchoredPosition = isFull ? _readyTweenSettings.endValue : _notReadyPosition;
            }
            else
            {
                ShowReady();
            }
        }
        _isFull = isFull;
    }

    public void Clear()
    {
        AnswerPeek = null;
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

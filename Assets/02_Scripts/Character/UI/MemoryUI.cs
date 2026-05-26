using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryUI : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private Image _answerTypeIcon;
    [SerializeField] private TMP_Text _answerID;

    [Header("Already Answered")]
    [SerializeField] private CanvasGroup _alreadyAnsweredFadeTarget;
    [SerializeField] private float _alreadyAnsweredFadeDelay;
    [SerializeField] private float _alreadyAnsweredFadeDuration;

    private IAnswerIconProvider _iconProvider;
    private Tween _alreadyAnsweredTween;

    public void Inject(IAnswerIconProvider iconProvider)
    {
        _iconProvider = iconProvider;
    }

    public void SetAnswerID(string value)
    {
        _answerID.text = value;

        if (_iconProvider != null)
        {
            _answerTypeIcon.sprite = _iconProvider.GetAnswerTypeIcon(value);
        }
    }

    public void SetPercent(float value)
    {
        _fill.fillAmount = value;
    }

    public void Show()
    {
        _alreadyAnsweredTween.Stop();
        _answerTypeIcon.enabled = true;
        gameObject.SetActive(true);

        if (_alreadyAnsweredFadeTarget == null) return;
        _alreadyAnsweredFadeTarget.alpha = 1f;
    }

    public void ShowAlreadyAnswered()
    {
        _alreadyAnsweredTween.Stop();
        _answerTypeIcon.enabled = false;
        gameObject.SetActive(true);

        if (_alreadyAnsweredFadeTarget == null) return;
        _alreadyAnsweredTween = Tween.Alpha(_alreadyAnsweredFadeTarget, endValue: 0f, _alreadyAnsweredFadeDuration, startDelay: _alreadyAnsweredFadeDelay);
    }

    public void Hide()
    {
        _alreadyAnsweredTween.Stop();
        gameObject.SetActive(false);
    }
}

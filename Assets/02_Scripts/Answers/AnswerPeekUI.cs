using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class AnswerPeekUI : MonoBehaviour
{
    [SerializeField] private Image _characterIcon;
    [SerializeField] private Image _archetypeIcon;
    [SerializeField] private Image _answerTypeIcon;
    [SerializeField] private Image _answerCloudIcon;
    [SerializeField] private RectTransform _readyObject;
    //[SerializeField] private Image _progressMask;
    //[SerializeField] private Image _progressFill;
    [Header("Configurations")]
    //[SerializeField] private Gradient _progressGradient;
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

    public void Setup(AnswerPeek answerPeek, Sprite characterIcon, Sprite archetypeIcon, Sprite answerTypeIcon)
    {
        AnswerPeek = answerPeek;
        _characterIcon.sprite = characterIcon;
        _archetypeIcon.sprite = archetypeIcon;
        _answerTypeIcon.sprite = answerTypeIcon;
        _answerCloudIcon.color = ChangeCloudColor(answerPeek);
        UpdateProgress(setup: true);
    }

    public void UpdateProgress(bool setup)
    {
        bool isFull = AnswerPeek.AnswerSheet.IsAnswerFull(AnswerPeek.AnswerID, out float progress, out _);
        if (!isFull)
        {
            //SetProgress(progress);
        }
        else
        {
            //SetProgress(1 - AnswerPeek.AnswerController.ValidatingPercent); // Go backwards
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

    public Color32 ChangeCloudColor(AnswerPeek answerPeek)
    { 
        var currentCorrectness = answerPeek.AnswerController.GetCorrectness(answerPeek.AnswerID);

       switch (currentCorrectness)
       {
           case 0: return new Color32(102, 35, 35, 255);
           case 0.5f: return new Color32(194, 176, 83, 255);
           case 1f: return new Color32(89, 155, 112, 255);
       }
       return new Color32(255, 255, 255, 255);
    }
    

    // private void SetProgress(float percent)
    // {
    //     _progressMask.fillAmount = percent;
    //     _progressFill.color = _progressGradient.Evaluate(percent);
    // }
}

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
    [SerializeField] private RectTransform _shakeContainer;

    [Header("Configurations")]
    //[SerializeField] private Gradient _progressGradient;
    [SerializeField] private Vector2 _notReadyPosition;
    [SerializeField] private TweenSettings<Vector2> _readyTweenSettings;
    private Vector2 _originalAnchoredPosition;
    private RectTransform _rect;
    private Tween _readyTween;
    private Sequence _highlightTween;
    private Sequence _shakeTween;
    private bool _isFull;

    public AnswerPeek AnswerPeek { get; private set; }

    private void Awake()
    {
        _readyTweenSettings.startFromCurrent = true;
        _rect = GetComponent<RectTransform>();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        PlayIntro();
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
        if (AnswerPeek == null) return;

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
                if (_readyObject == null) return;
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

        if(_readyObject == null)
        {
            Debug.LogError("Tween / Ready object is NULL");
            return;
        }
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

    private void PlayIntro()
    {
        // Ensure layout is updated first
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            _rect.parent as RectTransform);

        // Reset scale
        _rect.localScale = Vector3.one;

        // Start visually above
        _rect.localPosition += Vector3.up * 100f;
        _rect.localScale = Vector3.zero;

        Sequence.Create()
            .Group(Tween.LocalPositionY(
                _rect,
                _rect.localPosition.y - 100f,
                0.35f,
                Ease.OutCubic
            ))
            .Group(Tween.Scale(
                _rect,
                Vector3.one,
                0.35f,
                Ease.OutBack
            ));
    }
    public void PlayExitAnimation(System.Action onComplete)
    {
        Sequence.Create()
            .Chain(Tween.Scale(_rect, new Vector3(1.1f, 0.9f, 1f), 0.12f, Ease.OutQuad))
            .Chain(Tween.Scale(_rect, Vector3.zero, 0.2f, Ease.InBack))
            .OnComplete(() => onComplete?.Invoke());
    }
    public void PlayHighlight()
    {
        _highlightTween.Stop();
        _highlightTween = Sequence.Create()
            .Chain(Tween.Scale(_rect, new Vector3(1.15f, 1.15f, 1f), 0.1f, Ease.OutQuad))
            .Chain(Tween.Scale(_rect, Vector3.one, 0.15f, Ease.OutBack));
    }
    public void PlayShake()
    {
        _shakeTween.Stop();
        float amount = 6f;
        float duration = 0.06f;

        _shakeTween = Sequence.Create(cycles: -1)
            .Chain(Tween.LocalPositionX(_shakeContainer, amount, duration, Ease.OutQuad))
            .Chain(Tween.LocalPositionX(_shakeContainer, -amount, duration, Ease.OutQuad))
            .Chain(Tween.LocalPositionX(_shakeContainer, amount, duration, Ease.OutQuad))
            .Chain(Tween.LocalPositionX(_shakeContainer, -amount, duration, Ease.OutQuad))
            .Chain(Tween.LocalPositionX(_shakeContainer, 0f, duration, Ease.OutQuad));
    }

    public void StopShake()
    {
        _shakeTween.Stop();
        _shakeContainer.localPosition = Vector3.zero;
    }
}

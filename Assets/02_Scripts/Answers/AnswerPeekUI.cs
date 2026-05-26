using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public enum EPeekState { PartialInfo, FullInfo, RepeatedInfo }
public enum EDiagonalDirectionHint { Precise, Random, Both }

public class AnswerPeekUI : MonoBehaviour
{
    [SerializeField] private Image _characterIcon;
    [SerializeField] private Image _archetypeIcon;
    [SerializeField] private Image _answerTypeIcon;
    [SerializeField] private Image _answerDirectionIcon;
    [SerializeField] private Image _answerDirectionIcon2;
    [SerializeField] private Image _answerCloudIcon;
    [SerializeField] private GameObject _answerTypeRoot;
    [SerializeField] private RectTransform _readyObject;
    [SerializeField] private RectTransform _shakeContainer;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _completedStamp;

    [Header("Configurations")]
    //[SerializeField] private Gradient _progressGradient;
    [SerializeField] private Vector2 _notReadyPosition;
    [SerializeField] private TweenSettings<Vector2> _readyTweenSettings;
    [SerializeField] private float _completedAlpha = 0.75f;
    [SerializeField] private float _collapseDelay = 2f;
    [Tooltip("Both mode: minimum ratio (0–1) of an axis component to total XZ magnitude before that arrow is shown.")]
    [SerializeField] [Range(0f, 1f)] private float _bothAxisMinRatio = 0.2f;

    private Vector2 _originalAnchoredPosition;
    private RectTransform _rect;
    private Tween _readyTween;
    private Sequence _highlightTween;
    private Sequence _shakeTween;
    private bool _isFull;
    private bool _isCompleted;
    private EPeekState _state = EPeekState.FullInfo;
    private bool _directionHintActive;
    private bool _randomAxisIsHorizontal;

    public AnswerPeek AnswerPeek { get; private set; }
    public EPeekState State => _state;

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
        ApplyState();
        UpdateProgress(setup: true);
    }

    public void SetState(EPeekState state)
    {
        if (_state == state) return;
        if (_state == EPeekState.RepeatedInfo) return;
        _state = state;
        ApplyState();
    }

    private void ApplyState()
    {
        if (AnswerPeek == null) return;
        _answerCloudIcon.color = _state == EPeekState.FullInfo
            ? ChangeCloudColor(AnswerPeek)
            : new Color32(255, 255, 255, 255);
        _answerTypeIcon.enabled = _state != EPeekState.RepeatedInfo;
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
        _isCompleted = false;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (_completedStamp != null)
            _completedStamp.gameObject.SetActive(false);

        _answerTypeRoot.SetActive(true);
        _answerTypeIcon.enabled = true;
        _state = EPeekState.FullInfo;
        SetDirectionHint(null);

        AnswerPeek = null;
    }

    public void SetAnswerTypeIconEnabled(bool value)
    {
        _answerTypeIcon.enabled = value;
    }

    // Shows arrow pointing from this student toward correctWorldPos (XZ→XY mapping).
    // Pass null to revert to the answer type icon.
    public void SetDirectionHint(Vector3? correctWorldPos, EDiagonalDirectionHint mode = EDiagonalDirectionHint.Precise)
    {
        if (!correctWorldPos.HasValue)
        {
            _answerTypeIcon.gameObject.SetActive(true);
            _answerDirectionIcon.gameObject.SetActive(false);
            _answerDirectionIcon2.gameObject.SetActive(false);
            _directionHintActive = false;
            return;
        }

        if (!_directionHintActive && mode == EDiagonalDirectionHint.Random)
            _randomAxisIsHorizontal = Random.value > 0.5f;

        _directionHintActive = true;
        _answerTypeIcon.gameObject.SetActive(false);

        if (AnswerPeek == null) return;

        Vector3 dir = correctWorldPos.Value - AnswerPeek.AnswerController.transform.position;

        float xzMag = new Vector2(dir.x, dir.z).magnitude;
        bool validH = xzMag > 0f && Mathf.Abs(dir.x) / xzMag >= _bothAxisMinRatio;
        bool validV = xzMag > 0f && Mathf.Abs(dir.z) / xzMag >= _bothAxisMinRatio;

        switch (mode)
        {
            case EDiagonalDirectionHint.Precise:
                _answerDirectionIcon.gameObject.SetActive(true);
                _answerDirectionIcon2.gameObject.SetActive(false);
                _answerDirectionIcon.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg);
                break;

            case EDiagonalDirectionHint.Random:
            {
                bool useH = (validH && validV) ? _randomAxisIsHorizontal
                          : validH             ? true
                          : validV             ? false
                          : Mathf.Abs(dir.x) >= Mathf.Abs(dir.z); // fallback: dominant axis
                _answerDirectionIcon.gameObject.SetActive(true);
                _answerDirectionIcon2.gameObject.SetActive(false);
                _answerDirectionIcon.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, useH ? (dir.x >= 0f ? 0f : 180f)
                                                   : (dir.z >= 0f ? 90f : -90f));
                break;
            }

            case EDiagonalDirectionHint.Both:
                _answerDirectionIcon.gameObject.SetActive(validH);
                _answerDirectionIcon2.gameObject.SetActive(validV);
                if (validH) _answerDirectionIcon.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, dir.x >= 0f ? 0f : 180f);
                if (validV) _answerDirectionIcon2.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, dir.z >= 0f ? 90f : -90f);
                break;
        }
    }

    public void ShowReady()
    {
        _readyTween.Stop();

        if (_readyObject == null)
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
            case 0: return new Color32(183, 48, 48, 255);
            case 0.5f: return new Color32(217, 180, 62, 255);
            case 1f: return new Color32(92, 181, 95, 255);
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

    public void SetCompleted(bool completed)
    {
        if (_isCompleted == completed) return;

        _isCompleted = completed;
        if (_canvasGroup == null)
        {
            Debug.LogWarning("SetCompleted called but CanvasGroup is NULL on " + gameObject.name);
            return;
        }

        float targetAlpha = completed ? _completedAlpha : 1f;
        Tween.Alpha(_canvasGroup, targetAlpha, 0.2f);

        if (_completedStamp != null)
        {
            _completedStamp.gameObject.SetActive(completed);
            if (completed)
            {
                _completedStamp.rectTransform.localScale = Vector3.one * 2f;
                _completedStamp.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-20f, -10f));
                Tween.Scale(_completedStamp.rectTransform, Vector3.one, 0.25f, Ease.OutBack).OnComplete(() =>
                {
                    _answerTypeRoot.SetActive(false);

                    // Collapse after delay
                    Tween.Delay(_collapseDelay).OnComplete(() =>
                    {
                        PlayExitAnimation(() => Hide());
                    });
                });
            }
        }
    }
}

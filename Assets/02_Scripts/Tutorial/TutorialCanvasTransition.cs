using PrimeTween;
using UnityEngine;

public class TutorialCanvasTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TweenSettings<float> _showSettings;
    [SerializeField] private TweenSettings<float> _hideSettings;

    private Tween _tween;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        _showSettings.startFromCurrent = true;
        _hideSettings.startFromCurrent = true;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public void Show()
    {
        _tween.Stop();
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _tween = Tween.Alpha(_canvasGroup, _showSettings);
    }

    public void Hide()
    {
        _tween.Stop();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _tween = Tween.Alpha(_canvasGroup, _hideSettings);
    }
}

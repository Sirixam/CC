using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryUI : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private Image _answerTypeIcon;
    [SerializeField] private TMP_Text _answerID;

    private IAnswerIconProvider _iconProvider;

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
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

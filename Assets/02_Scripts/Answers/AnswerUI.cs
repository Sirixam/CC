using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _notFilledState;
    [SerializeField] private GameObject _filledState;
    [SerializeField] private Image _fill;

    public string ID { get; private set; }

    public void SetID(string value)
    {
        ID = value;
    }

    public void SetIcon(Sprite icon, Color color)
    {
        _icon.sprite = icon;
        _icon.color = color;
    }

    public void SetNumber(int value, Color color)
    {
        if (_numberText != null)
        {
            _numberText.text = value.ToString();
            _numberText.color = color;
        }
    }

    public void SetState(bool isFilled)
    {
        if (_notFilledState != null)
        {
            _notFilledState.SetActive(!isFilled);
        }
        if (_filledState != null)
        {
            _filledState.SetActive(isFilled);
        }
    }

    public void SetProgress(float percent)
    {
        if (_fill != null)
        {
            _fill.fillAmount = percent;
        }
    }
}

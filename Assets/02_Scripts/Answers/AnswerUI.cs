using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _notFilledState;
    [SerializeField] private GameObject _filledState;

    public void SetIcon(Sprite icon, Color color)
    {
        _icon.sprite = icon;
        _icon.color = color;
    }

    public void SetNumber(int value, Color color)
    {
        _numberText.text = value.ToString();
        _numberText.color = color;
    }

    public void SetState(bool isFilled)
    {
        _notFilledState.SetActive(!isFilled);
        _filledState.SetActive(isFilled);
    }
}

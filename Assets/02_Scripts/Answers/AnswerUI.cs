using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _notFilledState;
    [SerializeField] private GameObject _filledState;

    public void SetIcon(Sprite icon)
    {
        _icon.sprite = icon;
    }

    public void SetNumber(int value)
    {
        _numberText.text = value.ToString();
    }

    public void SetState(bool isFilled)
    {
        _notFilledState.SetActive(!isFilled);
        _filledState.SetActive(isFilled);
    }
}

using TMPro;
using UnityEngine;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField] private GameObject _notFilledState;
    [SerializeField] private GameObject _filledState;

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

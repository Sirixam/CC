using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryUI : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _answerNumberText;

    public void SetAnswerNumber(int value)
    {
        _answerNumberText.text = value.ToString();
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

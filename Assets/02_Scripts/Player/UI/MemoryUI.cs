using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryUI : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private Image _answerTypeIcon;
    [SerializeField] private TMP_Text _answerID;

    public void SetAnswerTypeIcon(Sprite icon)
    {
        _answerTypeIcon.sprite = icon;
    }

    public void SetAnswerID(string value)
    {
        _answerID.text = value;
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

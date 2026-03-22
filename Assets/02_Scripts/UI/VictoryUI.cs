using _02_Scripts.UI;
using UnityEngine;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private AnswerSheetsDisplayUI _answerSheetsDisplay;

    public void UpdateAnswerSheets()
    {
        _answerSheetsDisplay.UpdateAnswerSheets();
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

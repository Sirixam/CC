using UnityEngine;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private AnswerSheetUI[] _answerSheetsUI;

    public void UpdateAnswerSheets()
    {
        AnswerSheet[] answerSheets = AnswersManager.GetInstance().PlayerAnswerSheets;
        for (int i = 0; i < answerSheets.Length; i++)
        {
            _answerSheetsUI[i].Setup(answerSheets[i].Answers);
            _answerSheetsUI[i].Show();
        }
        for (int i = answerSheets.Length; i < _answerSheetsUI.Length; i++)
        {
            _answerSheetsUI[i].Hide();
        }
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

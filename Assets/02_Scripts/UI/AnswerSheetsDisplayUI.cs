using UnityEngine;

namespace _02_Scripts.UI

{
    public class AnswerSheetsDisplayUI : MonoBehaviour
    {
        [SerializeField] private AnswerSheetUI[] _answerSheetsUI;

        public void UpdateAnswerSheets()
        {
            AnswerSheet[] answerSheets = GameContext.AnswersManager.PlayerAnswerSheets;

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
    }
}
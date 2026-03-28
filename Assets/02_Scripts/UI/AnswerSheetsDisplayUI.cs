using UnityEngine;

namespace _02_Scripts.UI

{
    public class AnswerSheetsDisplayUI : MonoBehaviour
    {
        [SerializeField] private AnswerSheetUI[] _answerSheetsUI;

        public void UpdateAnswerSheets()
        {
            
            var players = GameManager.Instance.Players;
            Debug.Log($"[SHEETS] Players count: {players.Count}");
            for (int i = 0; i < players.Count; i++)
            {
                var answerSheet = GameManager.Instance.AnswerManager.GetPlayerSheet(i);
                Debug.Log($"[SHEETS] Player {i} answerSheet: {(answerSheet != null ? "found" : "NULL")}");
                if (answerSheet != null)
                {
                    _answerSheetsUI[i].Setup(answerSheet.Answers);
                    _answerSheetsUI[i].Show();
                }
                else
                {
                    _answerSheetsUI[i].Hide();
                }
            }

            for (int i = players.Count; i < _answerSheetsUI.Length; i++)
            {
                _answerSheetsUI[i].Hide();
            }
        }
    }
}
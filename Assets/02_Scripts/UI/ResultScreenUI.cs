using _02_Scripts._Uncategorized;
using _02_Scripts.Tools;
using _02_Scripts.Utils;
using TMPro;
using UnityEngine;

namespace _02_Scripts.UI
{
    public class ResultScreenUI : MonoBehaviour
    {
        [SerializeField] private ResultScreenDataSO victory;
        [SerializeField] private ResultScreenDataSO timesup;
        [SerializeField] private ResultScreenDataSO defeat;

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        
       //[SerializeField] private AnswerSheetsDisplayUI _answerSheetsDisplay;
       [SerializeField] private GradeDisplayUI _gradeDisplay; 
       [SerializeField] private TMP_Text _avgGradeResult;
       
        public void Show(ResultType type, float score)
        {
            float average = GradingHelper.GetAverageGrade(GameManager.Instance.Players, GameManager.Instance.AnswerManager);
            var (_, averageLetter) = GradingHelper.GetGrade(average / 100f);
            _avgGradeResult.text = averageLetter;
            
            switch (type)
            {
                case ResultType.Victory:
                    title.text = victory.title;
                    subtitle.text = victory.GetSubtitle(score);
                    
                    break;
                case ResultType.TimesUp:
                    title.text = timesup.title;
                    subtitle.text = timesup.GetSubtitle(score);
                    
                    break;
                case ResultType.Defeat:
                    title.text = defeat.title;
                    subtitle.text = defeat.GetSubtitle(score);
                    break;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowGrades()
        {
            _gradeDisplay.ShowGrades();
        }
        
        // public void UpdateAnswerSheets()
        // {
        //     _answerSheetsDisplay.UpdateAnswerSheets();
        // }
    }
}
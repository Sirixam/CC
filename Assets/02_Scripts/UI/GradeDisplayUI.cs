using _02_Scripts.Utils;
using UnityEngine;
using TMPro;

namespace _02_Scripts.UI
{
    public class GradeDisplayUI : MonoBehaviour
    {
        [SerializeField] private PlayerGradeUI _gradePrefab;
        [SerializeField] private Transform _container;

        public void ShowGrades()
        {
            // Clear old grades
            foreach (Transform child in _container)
            {
                Destroy(child.gameObject);
            }

            var players = GameManager.Instance.Players;
            var answersManager = GameManager.Instance.AnswerManager;
            var grades = GradingHelper.GetPlayerGrades(players, answersManager);

            foreach (var (playerName, letterGrade) in grades)
            {
                var gradeUI = Instantiate(_gradePrefab, _container);
                gradeUI.Setup(playerName, letterGrade);
            }
        }
    }
    
}
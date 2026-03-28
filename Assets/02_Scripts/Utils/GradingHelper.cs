using System.Collections.Generic;
using UnityEngine;

namespace _02_Scripts.Utils
{
    public class GradingHelper
    {
        public static (int numeric, string letter) GetGrade(float percentage)
        {
            int numeric = Mathf.RoundToInt(percentage * 10f);

            string letter =
                percentage >= 0.90f ? "A+" :
                percentage >= 0.85f ? "A" :
                percentage >= 0.80f ? "A-" :
                percentage >= 0.75f ? "B+" :
                percentage >= 0.70f ? "B" :
                percentage >= 0.65f ? "C+" :
                percentage >= 0.60f ? "C" :
                percentage >= 0.55f ? "D+" :
                percentage >= 0.50f ? "D" :
                percentage >= 0.40f ? "E" : "F";

            return (numeric, letter);
        }
        
        public static List<(string playerName, string letterGrade)> GetPlayerGrades(List<PlayerController> players, AnswersManager answersManager)
        {
            var results = new List<(string playerName, string letterGrade)>();

            for (int i = 0; i < players.Count; i++)
            {
                var answerSheet = answersManager.GetPlayerSheet(i);
                if (answerSheet == null)
                {
                    results.Add(($"Player {i + 1}", "?"));
                    continue;
                }

                var answers = answerSheet.Answers;
                float total = 0f;
                foreach (var answer in answers)
                {
                    total += answer.Correctness;
                }

                float percentage = Mathf.Clamp01(total / answers.Length + answerSheet.AccumulatedBoost);
                var (numeric, letter) = GetGrade(percentage);
                results.Add(($"Player {i + 1}", letter));
            }

            return results;
        }

        public static float GetAverageGrade(List<PlayerController> players, AnswersManager answersManager)
        {
            float totalClassPercentage = 0f;
            
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var answerSheet = answersManager.GetPlayerSheet(i);
                var answers = answerSheet.Answers;
                float total = 0f;

                foreach (var answer in answers)
                {
                    total += answer.Correctness; // 0, 0.5, 1
                }
                float percentage = Mathf.Clamp01(total / answers.Length + answerSheet.AccumulatedBoost);
                totalClassPercentage += percentage;
            }
            if (players.Count > 0)
            {
                float classAverage = totalClassPercentage / players.Count;
                return(classAverage * 100f);
            }
            return 0f;
        }
        

        public static void CalculateAndPrintGrades(List<PlayerController> players, AnswersManager answersManager)
        {

            float totalClassPercentage = 0f;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                var answerSheet = answersManager.GetPlayerSheet(i);
                if (answerSheet == null)
                {
                    Debug.LogWarning($"[GRADE] Player {player.name} has no AnswerSheet");
                    continue;
                }

                var answers = answerSheet.Answers;

                float total = 0f;

                foreach (var answer in answers)
                {
                    total += answer.Correctness; // 0, 0.5, 1
                }

                float percentage = Mathf.Clamp01(total / answers.Length + answerSheet.AccumulatedBoost);
                totalClassPercentage += percentage;

                var (numeric, letter) = GetGrade(percentage);

                Debug.Log($"[GRADE] {player.name} → {(percentage * 100f):F1}% | Grade: {numeric} ({letter})");
            }

            if (players.Count > 0)
            {
                float classAverage = totalClassPercentage / players.Count;
                var (avgNumeric, avgLetter) = GetGrade(classAverage);

                Debug.Log($"[CLASS AVG] {(classAverage * 100f):F1}% | Grade: {avgNumeric} ({avgLetter})");
            }
        }
    }
}
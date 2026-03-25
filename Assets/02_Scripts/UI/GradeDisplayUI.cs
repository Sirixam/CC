using UnityEngine;
using TMPro;

namespace _02_Scripts.UI
{
    public class GradeDisplayUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _gradeText;

        public void SetGrade(int numeric, string letter)
        {
            _gradeText.text = $"{numeric} ({letter})";
        }
    }
}
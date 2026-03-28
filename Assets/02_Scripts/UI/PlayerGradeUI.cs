using TMPro;
using UnityEngine;

namespace _02_Scripts.UI
{
    public class PlayerGradeUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _gradeText;

        public void Setup(string playerName, string grade)
        {
            _playerNameText.text = playerName;
            _gradeText.text = grade;
        }
    }
}
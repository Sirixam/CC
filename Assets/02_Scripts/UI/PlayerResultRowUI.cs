using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResultRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private Transform _answerCellsContainer;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private Image _answerCellPrefab;

    [Header("Colors")]
    [SerializeField] private Color _correctColor = new Color(0.36f, 0.71f, 0.37f);
    [SerializeField] private Color _halfCorrectColor = new Color(0.85f, 0.71f, 0.24f);
    [SerializeField] private Color _incorrectColor = new Color(0.71f, 0.19f, 0.19f);
    [SerializeField] private Color _emptyColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Icons")]
    [SerializeField] private Sprite _correctSprite;
    [SerializeField] private Sprite _halfCorrectSprite;
    [SerializeField] private Sprite _incorrectSprite;

    public void Setup(string playerName, Answer[] answers, string letterGrade)
    {
        _playerNameText.text = playerName;
        _gradeText.text = letterGrade;

        foreach (Transform child in _answerCellsContainer)
            Destroy(child.gameObject);

        foreach (var answer in answers)
        {
            Image cell = Instantiate(_answerCellPrefab, _answerCellsContainer);

            Image icon = cell.transform.Find("AnswerImage")?.GetComponent<Image>();
            Debug.Log($"Icon found: {icon}");
            Debug.Log($"Icon activeSelf: {icon?.gameObject.activeSelf}");
            Debug.Log($"Icon activeInHierarchy: {icon?.gameObject.activeInHierarchy}");
            
            Transform t = icon.transform;
            while (t != null)
            {
                Debug.Log($"{t.name} | activeSelf={t.gameObject.activeSelf} activeInHierarchy={t.gameObject.activeInHierarchy}");
                t = t.parent;
            }
            cell.enabled = true;

            if (icon != null)
                icon.enabled = true;

            if (!answer.IsAnswerFull)
            {
                icon.color = _emptyColor;

                if (icon != null)
                    icon.gameObject.SetActive(false);
            }
            else if (answer.Correctness >= 1f)
            {
                icon.color = _correctColor;

                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    icon.sprite = _correctSprite;
                }
            }
            else if (answer.Correctness > 0f)
            {
                icon.color = _halfCorrectColor;

                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    icon.sprite = _halfCorrectSprite;
                }
            }
            else
            {
                icon.color = _incorrectColor;

                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    icon.sprite = _incorrectSprite;
                }
            }
        }
    }
}
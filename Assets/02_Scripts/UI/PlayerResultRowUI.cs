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

            // Find the background and icon child images by name
            Image background = cell.transform.Find("AnswerCellBackground")?.GetComponent<Image>();
            Image icon = cell.transform.Find("AnswerImage")?.GetComponent<Image>();

            if (!answer.IsAnswerFull)
            {
                if (background != null) background.color = _emptyColor;
                if (icon != null) icon.gameObject.SetActive(false);
            }
            else if (answer.Correctness >= 1f)
            {
                if (background != null) background.color = _correctColor;
                if (icon != null) { icon.gameObject.SetActive(true); icon.sprite = _correctSprite; }
            }
            else if (answer.Correctness > 0f)
            {
                if (background != null) background.color = _halfCorrectColor;
                if (icon != null) { icon.gameObject.SetActive(true); icon.sprite = _halfCorrectSprite; }
            }
            else
            {
                if (background != null) background.color = _incorrectColor;
                if (icon != null) { icon.gameObject.SetActive(true); icon.sprite = _incorrectSprite; }
            }
        }
    }
}
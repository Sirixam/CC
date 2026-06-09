using System;
using _02_Scripts._Uncategorized;
using _02_Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;

public class ExamResultScreenUI : MonoBehaviour
{
    [SerializeField] private PlayerResultRowUI _rowPrefab;
    [SerializeField] private Transform _rowsContainer;
    [SerializeField] private Transform _iconHeaderContainer;
    [SerializeField] private Image _answerIconPrefab;

    [Header("Level Navigation")]
    [SerializeField] private ButtonListener _nextLevelButton;
    [SerializeField] private ButtonListener _prevLevelButton;

    private bool _headerBuilt = false;

    public void SetupNavigation(Action onNext, Action onPrev)
    {
        if (_nextLevelButton != null)
            _nextLevelButton.OnClickEvent = onNext;
        if (_prevLevelButton != null)
            _prevLevelButton.OnClickEvent = onPrev;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (_nextLevelButton != null)
            _nextLevelButton.gameObject.SetActive(GameManager.Instance.HasNextLevel);
        if (_prevLevelButton != null)
            _prevLevelButton.gameObject.SetActive(GameManager.Instance.HasPreviousLevel);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowGrades()
    {
        if (!_headerBuilt)
        {
            BuildHeader(GameManager.Instance.AnswerManager.PlayerAnswerDefinitions);
            _headerBuilt = true;
        }

        ShowResults();
    }

    private void BuildHeader(AnswerDefinition[] answerDefinitions)
    {
        foreach (Transform child in _iconHeaderContainer)
            Destroy(child.gameObject);

        foreach (var def in answerDefinitions)
        {
            Image icon = Instantiate(_answerIconPrefab, _iconHeaderContainer);
            icon.sprite = def.Icon;
            icon.color = Color.black;
            
            icon.enabled = true;
        }
    }

    private void ShowResults()
    {
        foreach (Transform child in _rowsContainer)
            Destroy(child.gameObject);

        var players = GameManager.Instance.Players;
        var answersManager = GameManager.Instance.AnswerManager;
        var grades = GradingHelper.GetPlayerGrades(players, answersManager);

        for (int i = 0; i < players.Count; i++)
        {
            var sheet = answersManager.GetPlayerSheet(i);
            if (sheet == null) continue;

            string letter = i < grades.Count ? grades[i].letterGrade : "?";
            PlayerResultRowUI row = Instantiate(_rowPrefab, _rowsContainer);
            row.Setup($"Player {i + 1}", sheet.Answers, letter);
        }
    }
}
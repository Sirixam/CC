using System;
using System.Collections.Generic;
using UnityEngine;

public class AnswerSheetUI : MonoBehaviour
{
    [SerializeField] private AnswerUI _answerPrefab;
    [SerializeField] private Transform _answersParent;
    [SerializeField] private AnswerProgressUI _progress;

    private List<AnswerUI> _answers = new();

    private void Awake()
    {
        _progress.Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Setup(Answer[] answers)
    {
        CleanAnswers();
        for (int i = 0; i < answers.Length; i++)
        {
            int answerNumber = i + 1;
            _answers.Add(InstantiateAnswer(answers[i].ID, answerNumber, answers[i].Icon, answers[i].Color, isFilled: false));
        }
    }

    public void ShowProgress(float percent = 0)
    {
        _progress.SetPercent(percent);
        _progress.Show();
    }

    public void SetProgress(float percent)
    {
        _progress.SetPercent(percent);
    }

    public void HideProgress()
    {
        _progress.Hide();
    }

    public void SetAnswerState(string answerID, bool isFilled)
    {
        AnswerUI answerUI = _answers.Find(x => x.ID == answerID);
        answerUI.SetState(isFilled);
    }

    private AnswerUI InstantiateAnswer(string answerID, int number, Sprite icon, Color color, bool isFilled)
    {
        AnswerUI instance = Instantiate(_answerPrefab, _answersParent);
        instance.SetID(answerID);
        instance.SetNumber(number, color);
        instance.SetIcon(icon, color);
        instance.SetState(isFilled);
        return instance;
    }

    public void ResetAnswerStates()
    {
        foreach (var answer in _answers)
        {
            answer.SetState(isFilled: false);
        }
    }

    private void CleanAnswers()
    {
        foreach (var answer in _answers)
        {
            Destroy(answer.gameObject);
        }
        _answers.Clear();
    }
}

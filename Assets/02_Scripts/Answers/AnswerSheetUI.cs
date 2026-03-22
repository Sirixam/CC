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
        if (_progress != null)
        {
            _progress.Hide();
        }
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
            Answer answer = answers[i];
            _answers.Add(InstantiateAnswer(answer.ID, answerNumber, answer.Icon, answer.Color, answer.IsAnswerFull, answer.Correctness));
        }
    }

    public void ShowProgress(string answerID, float percent = 0)
    {
        if (_progress != null)
        {
            _progress.SetPercent(percent);
            _progress.Show();
        }
        else
        {
            AnswerUI answerUI = _answers.Find(x => x.ID == answerID);
            answerUI.SetProgress(percent);
        }
    }

    public void SetProgress(string answerID, float percent)
    {
        if (_progress != null)
        {
            _progress.SetPercent(percent);
        }
        else
        {
            AnswerUI answerUI = _answers.Find(x => x.ID == answerID);
            answerUI.SetProgress(percent);
        }
    }

    public void HideProgress(string answerID = null)
    {
        if (_progress != null)
        {
            _progress.Hide();
        }
        else if (answerID != null)
        {
            AnswerUI answerUI = _answers.Find(x => x.ID == answerID);
            answerUI.SetProgress(0);
        }
        else
        {
            foreach (var answerUI in _answers)
            {
                answerUI.SetProgress(0);
            }
        }
    }

    public void SetCorrectness(string answerID, float correctness)
    {
        AnswerUI answerUI = _answers.Find(x => x.ID == answerID);
        answerUI.SetCorrectness(correctness);
    }

    public void SetAnswerState(string answerID, bool isFilled)
    {
        AnswerUI answerUI = _answers.Find(x => x.ID == answerID);
        answerUI.SetState(isFilled);
    }

    private AnswerUI InstantiateAnswer(string answerID, int number, Sprite icon, Color color, bool isFilled, float correctness)
    {
        AnswerUI instance = Instantiate(_answerPrefab, _answersParent);
        instance.SetID(answerID);
        instance.SetNumber(number, color);
        instance.SetIcon(icon, color);
        instance.SetFinalState(isFilled, correctness);
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

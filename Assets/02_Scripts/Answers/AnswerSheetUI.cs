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

    public void Setup(int answersCount)
    {
        CleanAnswers();
        for (int i = 0; i < answersCount; i++)
        {
            int answerNumber = i + 1;
            _answers.Add(InstantiateAnswer(answerNumber, isFilled: false));
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

    public void SetAnswerState(int index, bool isFilled)
    {
        _answers[index].SetState(isFilled);
    }

    private AnswerUI InstantiateAnswer(int number, bool isFilled)
    {
        AnswerUI instance = Instantiate(_answerPrefab, _answersParent);
        instance.SetNumber(number);
        instance.SetState(isFilled);
        return instance;
    }

    private void CleanAnswers()
    {
        foreach (var answer in _answers)
        {
            Destroy(answer.gameObject);
        }
        _answers.Clear();
    }

    [Button("Create Answers")]
    private void EDITOR_CreateAnswers()
    {
        Setup(10);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class AnswersSheetUI : MonoBehaviour
{
    [SerializeField] private AnswerUI _answerPrefab;
    [SerializeField] private Transform _answersParent;
    [SerializeField] private ProgressUI _progress;

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

    public void Setup(bool[] answers)
    {
        CleanAnswers();
        for (int i = 0; i < answers.Length; i++)
        {
            _answers.Add(InstantiateAnswer(i + 1, answers[i]));
        }
    }

    public void SetProgress(float percent)
    {
        _progress.SetPercent(percent);
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
        Setup(new bool[10]);
    }
}

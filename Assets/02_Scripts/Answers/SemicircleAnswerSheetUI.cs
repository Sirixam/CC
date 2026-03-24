using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemicircleAnswerSheetUI : MonoBehaviour, IAnswerSheetUI
{
    [SerializeField] AnswerUI _answerPrefab;
    [SerializeField] private Transform _answersParent;
    [SerializeField] private float _radius;
    [SerializeField] private float _angleRange;
        [SerializeField] private AnswerProgressUI _progress;

    private List<AnswerUI> _answers = new();


    public void ShowProgress(string answerID, float percent = 0)
    {
        
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
    
    public void HideProgress(string answerID)
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

    public void HideProgress()
    {
        HideProgress(null);
    }
    public void Show()
    {
        gameObject.SetActive(true);

    }

    public void Hide()
    {
        gameObject.SetActive(false);

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

    public void Setup(Answer[] answers)
    {
        CleanAnswers();
        for (int i = 0; i < answers.Length; i++)
        {
            int answerNumber = i + 1;
            Answer answer = answers[i];
            _answers.Add(InstantiateAnswer(answer.ID, answerNumber, answer.Icon, answer.Color, answer.IsAnswerFull, answer.Correctness));
        }

        PositionAnswers();
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

    private void CleanAnswers()
    {
        foreach (var answer in _answers)
        {
            Destroy(answer.gameObject);
        }
        _answers.Clear();
    }

    private void PositionAnswers()
    {
        float startAngle;
        float step;

        if (_answers.Count == 1)
        {
            startAngle = 0;
            step = 0;
        }
        else
        {
            startAngle = -_angleRange / 2;
            step = _angleRange / (_answers.Count - 1);
        }

        for (int i = 0; i < _answers.Count; i++)
        {
            var angle = Mathf.Deg2Rad * (startAngle + step * i);
            var x = Mathf.Sin(angle) * _radius;
            var y = Mathf.Cos(angle) * _radius;
            _answers[i].transform.localPosition = new Vector3(x, y, 0);
        }
    }

    public void ResetAnswerStates()
    {
        foreach (var answer in _answers)
        {
            answer.SetState(isFilled: false);
        }
    }

}

using System;
using UnityEngine;

public class DeskController : MonoBehaviour
{
    [Serializable]
    public class Data
    {
        public int AnswersCount;
        public float AnswerDuration;
        public bool PersistProgress;
    }

    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;
    [SerializeField] private AnswersSheetUI _answersSheetUI;

    private Data _data;
    private int _activeAnswerNumber;
    private float[] _answersProgress;

    public bool HasAnswers => _answersProgress != null;
    public bool IsAnswering => _activeAnswerNumber > 0;

    public Transform LookAtPoint => _lookAtPoint;
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;

    private void Awake()
    {
        _answersSheetUI.Hide();
    }

    public void Setup(Data data)
    {
        _data = data;
        _answersProgress = new float[data.AnswersCount];
        _answersSheetUI.Setup(data.AnswersCount);
    }

    public void ShowAnswersSheet()
    {
        if (!HasAnswers) return;
        _answersSheetUI.Show();
    }

    public bool TryStartAnswering(int answerNumber)
    {
        if (!HasAnswers) return false; // No answer sheet in this desk

        float progress = _answersProgress[answerNumber - 1];
        if (progress >= 1) return false; // Already answered

        _activeAnswerNumber = answerNumber;

        _answersSheetUI.ShowProgress(progress);
        _answersSheetUI.Show();
        return true;
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        float progressDelta = Time.deltaTime / _data.AnswerDuration;
        int answerIndex = _activeAnswerNumber - 1;
        _answersProgress[answerIndex] = Mathf.Clamp01(_answersProgress[answerIndex] + progressDelta);
        finishedAnswering = _answersProgress[answerIndex] >= 1;
        _answersSheetUI.SetProgress(_answersProgress[answerIndex]);
        if (finishedAnswering)
        {
            _answersSheetUI.SetAnswerState(answerIndex, true);
            _answersSheetUI.HideProgress();
            _activeAnswerNumber = 0;
        }
    }

    public void HideAnswersSheet()
    {
        if (!HasAnswers) return;
        _answersSheetUI.Hide();

        if (IsAnswering)
        {
            _answersSheetUI.HideProgress();
            if (!_data.PersistProgress)
            {
                _answersProgress[_activeAnswerNumber - 1] = 0;
            }
            _activeAnswerNumber = 0;
        }
    }
}

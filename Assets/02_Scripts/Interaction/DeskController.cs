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
    [SerializeField] private AnswersSheetUI _answersSheetUI;
    [SerializeField] private ChairController _chairController;
    [SerializeField] private InteractionController _interactionController;

    private Data _data;
    private int _activeAnswerNumber;
    private float[] _answersProgress;

    public int PlayerIndex { get; private set; }

    public bool HasAnswers => _answersProgress != null;
    public bool IsAnswering => _activeAnswerNumber > 0;
    public bool IsPlayerDesk => PlayerIndex >= 0;

    public Transform LookAtPoint => _lookAtPoint;

    public event Action<DeskController> OnFinishAnsweringEvent;

    private void Awake()
    {
        _answersSheetUI.Hide();
    }

    public int GetFullAnswersCount()
    {
        return Array.FindAll(_answersProgress, x => x >= 1).Length;
    }

    public bool IsAnswerFull(int answerNumber)
    {
        return _answersProgress[answerNumber - 1] >= 1;
    }

    public void Setup(Data data, int playerIndex, bool canUseAnyPlayerChair)
    {
        _data = data;
        PlayerIndex = playerIndex;
        _answersProgress = new float[data.AnswersCount];
        _answersSheetUI.Setup(data.AnswersCount);
        _chairController.Setup(this, canUseAnyPlayerChair); // Trigger after setting player index
        if (IsPlayerDesk)
        {
            _interactionController.Disable();
        }
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
            OnFinishAnsweringEvent?.Invoke(this);
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

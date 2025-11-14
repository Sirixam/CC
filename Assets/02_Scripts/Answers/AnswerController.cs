using System;
using UnityEngine;

public class AnswerController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private AnswerSheetUI _answerSheetUI;
    [SerializeField] private InteractionController _interactionController;

    private AnswerSheet _answerSheet;

    public int ActiveAnswerNumber { get; private set; }
    public int PlayerIndex { get; private set; }

    private bool CanShowUI => IsPlayer;
    private bool HasAnswerSheet => _answerSheet != null;
    public bool IsAnswering => ActiveAnswerNumber > 0;
    public bool IsPlayer => PlayerIndex >= 0;

    public Transform LookAtPoint => _lookAtPoint;

    public event Action<AnswerController, int> OnFinishAnsweringEvent;

    private void Awake()
    {
        _answerSheetUI.Hide();
    }

    public void Setup(AnswerSheet answerSheet, int playerIndex)
    {
        _answerSheet = answerSheet;
        PlayerIndex = playerIndex;
        if (answerSheet != null && CanShowUI)
        {
            _answerSheetUI.Setup(answerSheet.Answers);
        }
        if (IsPlayer)
        {
            _interactionController.Disable();
        }
    }

    public void ShowAnswerSheet()
    {
        if (!HasAnswerSheet || !CanShowUI) return;
        _answerSheetUI.Show();
    }

    public bool TryRestartAnswering(int answerNumber)
    {
        if (!HasAnswerSheet || !_answerSheet.HasAnswer(answerNumber - 1)) return false; // No answer sheet in this desk
        _answerSheet.ResetProgress(answerNumber - 1);
        StartAnswering(answerNumber, progress: 0);
        return true;
    }

    public bool TryStartAnswering(int answerNumber)
    {
        if (!HasAnswerSheet || !_answerSheet.HasAnswer(answerNumber - 1)) return false; // No answer sheet in this desk
        if (_answerSheet.IsAnswerFull(answerNumber - 1, out float progress)) return false; // Already answered
        StartAnswering(answerNumber, progress);
        return true;
    }

    private void StartAnswering(int answerNumber, float progress)
    {
        ActiveAnswerNumber = answerNumber;
        if (CanShowUI)
        {
            _answerSheetUI.ShowProgress(progress);
            _answerSheetUI.Show();
        }
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        int answerIndex = ActiveAnswerNumber - 1;
        float progress = _answerSheet.UpdateProgress(answerIndex, out finishedAnswering);
        if (CanShowUI)
        {
            _answerSheetUI.SetProgress(progress);
        }
        if (finishedAnswering)
        {
            if (CanShowUI)
            {
                _answerSheetUI.SetAnswerState(answerIndex, true);
                _answerSheetUI.HideProgress();
            }
            ActiveAnswerNumber = 0;
            OnFinishAnsweringEvent?.Invoke(this, answerIndex + 1);
        }
    }

    public void HideAnswerSheet()
    {
        if (!HasAnswerSheet) return;

        if (CanShowUI)
        {
            _answerSheetUI.Hide();
        }
        if (IsAnswering)
        {
            if (CanShowUI)
            {
                _answerSheetUI.HideProgress();
            }
            _answerSheet.OnStopAnswering(ActiveAnswerNumber - 1);
            ActiveAnswerNumber = 0;
        }
    }
}

using System;
using UnityEngine;

public class DeskController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private AnswerSheetUI _answerSheetUI;
    [SerializeField] private ChairController _chairController;
    [SerializeField] private InteractionController _interactionController;

    private int _activeAnswerNumber;
    private AnswerSheet _answerSheet;

    public int PlayerIndex { get; private set; }

    public bool HasAnswerSheet => _answerSheet != null;
    public bool IsAnswering => _activeAnswerNumber > 0;
    public bool IsPlayerDesk => PlayerIndex >= 0;

    public Transform LookAtPoint => _lookAtPoint;

    public event Action<DeskController, int> OnFinishAnsweringEvent;

    private void Awake()
    {
        _answerSheetUI.Hide();
    }

    public void Setup(AnswerSheet answerSheet, int playerIndex, bool canUseAnyPlayerChair)
    {
        _answerSheet = answerSheet;
        PlayerIndex = playerIndex;
        if (answerSheet != null)
        {
            _answerSheetUI.Setup(answerSheet.AnswersCount);
        }
        _chairController.Setup(this, canUseAnyPlayerChair); // Trigger after setting player index
        if (IsPlayerDesk)
        {
            _interactionController.Disable();
        }
    }

    public void ShowAnswerSheet()
    {
        if (!HasAnswerSheet) return;
        _answerSheetUI.Show();
    }

    public bool TryStartAnswering(int answerNumber)
    {
        if (!HasAnswerSheet) return false; // No answer sheet in this desk
        if (_answerSheet.IsAnswerFull(answerNumber - 1, out float progress)) return false; // Already answered

        _activeAnswerNumber = answerNumber;
        _answerSheetUI.ShowProgress(progress);
        _answerSheetUI.Show();
        return true;
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        int answerIndex = _activeAnswerNumber - 1;
        float progress = _answerSheet.UpdateProgress(answerIndex, out finishedAnswering);
        _answerSheetUI.SetProgress(progress);
        if (finishedAnswering)
        {
            _answerSheetUI.SetAnswerState(answerIndex, true);
            _answerSheetUI.HideProgress();
            _activeAnswerNumber = 0;
            OnFinishAnsweringEvent?.Invoke(this, answerIndex + 1);
        }
    }

    public void HideAnswerSheet()
    {
        if (!HasAnswerSheet) return;
        _answerSheetUI.Hide();

        if (IsAnswering)
        {
            _answerSheetUI.HideProgress();
            _answerSheet.OnStopAnswering(_activeAnswerNumber - 1);
            _activeAnswerNumber = 0;
        }
    }
}

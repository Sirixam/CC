using System;
using UnityEngine;

public class AnswerController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private AnswerSheetUI _answerSheetUI;
    [SerializeField] private InteractionController _interactionController;

    private int _cheatBlockCount;

    public AnswerSheet AnswerSheet { get; private set; }
    public string ActiveAnswerID { get; private set; }
    public string LastFinishedAnswerID { get; private set; }
    public string ActorID { get; private set; }
    public bool IsPlayer { get; private set; }
    public bool IsCheatBlocked => _cheatBlockCount <= 0;

    private bool HasAnswerSheet => AnswerSheet != null;
    public bool IsAnswering => !string.IsNullOrWhiteSpace(ActiveAnswerID);
    public bool IsValidatingAnswer => HasAnswerSheet && !IsAnswering && AnswerSheet.IsAnswerFull(LastFinishedAnswerID, out _);

    public Transform LookAtPoint => _lookAtPoint;

    public event Action<AnswerController, string> OnFinishPeekingEvent;
    public event Action<AnswerController, string> OnFinishAnsweringEvent;

    private void Awake()
    {
        _answerSheetUI.Hide();
    }

    public void Setup(AnswerSheet answerSheet, string actorID, bool isPlayer)
    {
        AnswerSheet = answerSheet;
        ActorID = actorID;
        IsPlayer = isPlayer;
        if (answerSheet != null && IsPlayer)
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
        if (!HasAnswerSheet || !IsPlayer) return;
        _answerSheetUI.Show();
    }

    public bool TryRestartAnswering(string answerID, bool isThinking)
    {
        if (!HasAnswerSheet || !AnswerSheet.HasAnswer(answerID)) return false; // No answer sheet in this desk
        AnswerSheet.ResetProgress(answerID);
        ActiveAnswerID = answerID;
        if (!isThinking)
        {
            StartAnswering(progress: 0);
        }
        return true;
    }

    public bool TryStartAnswering(string answerID)
    {
        if (!HasAnswerSheet || !AnswerSheet.HasAnswer(answerID)) return false; // No answer sheet in this desk
        if (AnswerSheet.IsAnswerFull(answerID, out float progress)) return false; // Already answered
        ActiveAnswerID = answerID;
        StartAnswering(progress);
        return true;
    }

    public void StartAnswering(float progress)
    {
        if (IsPlayer)
        {
            _answerSheetUI.ShowProgress(progress);
            _answerSheetUI.Show();
        }
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        string answerID = ActiveAnswerID;
        float progress = AnswerSheet.UpdateProgress(answerID, out finishedAnswering);
        if (IsPlayer)
        {
            _answerSheetUI.SetProgress(progress);
        }
        if (finishedAnswering)
        {
            if (IsPlayer)
            {
                _answerSheetUI.SetAnswerState(answerID, true);
                _answerSheetUI.HideProgress();
            }
            LastFinishedAnswerID = answerID;
            ActiveAnswerID = null;
            OnFinishAnsweringEvent?.Invoke(this, answerID);
        }
    }

    public void HideAnswerSheet()
    {
        if (!HasAnswerSheet) return;

        if (IsPlayer)
        {
            _answerSheetUI.Hide();
        }
        if (IsAnswering)
        {
            if (IsPlayer)
            {
                _answerSheetUI.HideProgress();
            }
            string answerID = ActiveAnswerID;
            ActiveAnswerID = null;
            AnswerSheet.OnStopAnswering(answerID);
        }
    }

    public void TriggerFinishedPeeking()
    {
        string answerID = IsAnswering ? ActiveAnswerID : LastFinishedAnswerID;
        OnFinishPeekingEvent?.Invoke(this, answerID);
    }

    public void ResetProgress()
    {
        if (!HasAnswerSheet) return;
        AnswerSheet.ResetProgress();
        if (IsPlayer)
        {
            _answerSheetUI.HideProgress();
            _answerSheetUI.ResetAnswerStates();
        }
    }

    public void BlockCheat()
    {
        _cheatBlockCount++;
    }

    public void UnblockCheat()
    {
        _cheatBlockCount--;
    }
}

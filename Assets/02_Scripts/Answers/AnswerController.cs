using System;
using TMPro;
using UnityEngine;

public class AnswerController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private AnswerSheetUI _answerSheetUI;
    [SerializeField] private InteractionController _interactionController;
    [SerializeField] private TMP_Text _stateText;

    public AnswerSheet AnswerSheet { get; private set; }
    public int ActiveAnswerNumber { get; private set; }
    public int LastFinishedAnswerNumber { get; private set; }
    public string ActorID { get; private set; }
    public bool IsPlayer { get; private set; }

    private bool HasAnswerSheet => AnswerSheet != null;
    public bool IsAnswering => ActiveAnswerNumber > 0;
    public bool IsCheckingAnswer => !IsAnswering && AnswerSheet.IsAnswerFull(LastFinishedAnswerNumber - 1, out _);

    public Transform LookAtPoint => _lookAtPoint;

    public event Action<AnswerController, int> OnFinishPeekingEvent;
    public event Action<AnswerController, int> OnFinishAnsweringEvent;

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
            _stateText.text = string.Empty;
            _interactionController.Disable();
        }
        else
        {
            _stateText.text = "Idle";
        }
    }

    public void ShowAnswerSheet()
    {
        if (!HasAnswerSheet || !IsPlayer) return;
        _answerSheetUI.Show();
    }

    public bool TryRestartAnswering(int answerNumber, bool isThinking)
    {
        if (!HasAnswerSheet || !AnswerSheet.HasAnswer(answerNumber - 1)) return false; // No answer sheet in this desk
        AnswerSheet.ResetProgress(answerNumber - 1);
        ActiveAnswerNumber = answerNumber;
        if (!isThinking)
        {
            StartAnswering(progress: 0);
        }
        else if (!IsPlayer)
        {
            _stateText.text = "Thinking";
        }
        return true;
    }

    public bool TryStartAnswering(int answerNumber)
    {
        if (!HasAnswerSheet || !AnswerSheet.HasAnswer(answerNumber - 1)) return false; // No answer sheet in this desk
        if (AnswerSheet.IsAnswerFull(answerNumber - 1, out float progress)) return false; // Already answered
        ActiveAnswerNumber = answerNumber;
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
        else
        {
            _stateText.text = "Answering";
        }
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        int answerIndex = ActiveAnswerNumber - 1;
        float progress = AnswerSheet.UpdateProgress(answerIndex, out finishedAnswering);
        if (IsPlayer)
        {
            _answerSheetUI.SetProgress(progress);
        }
        if (finishedAnswering)
        {
            if (IsPlayer)
            {
                _answerSheetUI.SetAnswerState(answerIndex, true);
                _answerSheetUI.HideProgress();
            }
            else
            {
                _stateText.text = "Checking";
            }
            LastFinishedAnswerNumber = ActiveAnswerNumber;
            ActiveAnswerNumber = 0;
            OnFinishAnsweringEvent?.Invoke(this, answerIndex + 1);
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
            AnswerSheet.OnStopAnswering(ActiveAnswerNumber - 1);
            ActiveAnswerNumber = 0;
        }
    }

    public void TriggerFinishedPeeking()
    {
        int answerNumber = IsAnswering ? ActiveAnswerNumber : LastFinishedAnswerNumber;
        OnFinishPeekingEvent?.Invoke(this, answerNumber);
    }
}

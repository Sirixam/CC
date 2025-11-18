using System;
using TMPro;
using UnityEngine;

public class AnswerController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private AnswerSheetUI _answerSheetUI;
    [SerializeField] private InteractionController _interactionController;
    [SerializeField] private TMP_Text _stateText;

    private AnswerSheet _answerSheet;
    private int _lastFinishedAnswerNumber;

    public int ActiveAnswerNumber { get; private set; }
    public int PlayerIndex { get; private set; }

    private bool HasAnswerSheet => _answerSheet != null;
    public bool IsAnswering => ActiveAnswerNumber > 0;
    public bool IsCheckingAnswer => !IsAnswering && _answerSheet.IsAnswerFull(_lastFinishedAnswerNumber - 1, out _);
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
        float progress = _answerSheet.UpdateProgress(answerIndex, out finishedAnswering);
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
            _lastFinishedAnswerNumber = ActiveAnswerNumber;
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
            _answerSheet.OnStopAnswering(ActiveAnswerNumber - 1);
            ActiveAnswerNumber = 0;
        }
    }
}

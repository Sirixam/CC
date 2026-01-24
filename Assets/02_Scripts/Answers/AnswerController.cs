using System;
using UnityEngine;

public class AnswerController : MonoBehaviour
{
    public enum EState
    {
        Idle,
        Thinking,
        Answering,
        Validating,
    }

    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private AnswerSheetUI _answerSheetUI;
    [SerializeField] private InteractionController _interactionController;

    private int _cheatBlockCount;
    private EState _state;
    private bool HasAnswerSheet => AnswerSheet != null;
    private float _thinkingDuration;
    private float _thinkingRemainingTime;
    private float _answeringDuration;
    private float _answeringRemainingTime;
    private float _validatingDuration;
    private float _validatingRemainingTime;

    public AnswerSheet AnswerSheet { get; private set; }
    public string ActiveAnswerID { get; private set; }
    public string LastFinishedAnswerID { get; private set; }
    public string ActorID { get; private set; }
    public bool IsPlayer { get; private set; }
    public bool IsCheatBlocked => _cheatBlockCount > 0;
    public bool IsThinking => _state == EState.Thinking;
    public bool IsAnswering => _state == EState.Answering;
    public bool IsValidating => _state == EState.Validating;
    public float ThinkingPercent => 1f - _thinkingRemainingTime / _thinkingDuration;
    public float AnsweringPercent => 1f - _answeringRemainingTime / _answeringDuration;
    public float ValidatingPercent => 1f - _validatingRemainingTime / _validatingDuration;
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

    public void UpdateRemainingTime(float deltaTime, out bool finished)
    {
        if (_state == EState.Thinking)
        {
            _thinkingRemainingTime = Mathf.Max(0, _thinkingRemainingTime - deltaTime);
            finished = _thinkingRemainingTime == 0;
        }
        else if (_state == EState.Answering)
        {
            _answeringRemainingTime = Mathf.Max(0, _answeringRemainingTime - deltaTime);
            finished = _answeringRemainingTime == 0;
        }
        else if (_state == EState.Validating)
        {
            _validatingRemainingTime = Mathf.Max(0, _validatingRemainingTime - deltaTime);
            finished = _validatingRemainingTime == 0;
        }
        else
        {
            finished = true;
            Debug.LogError("State is not being handled: " + _state);
        }
    }

    public void SetDurations(float thinkingDuration, float answeringDuration, float validatingDuration)
    {
        _thinkingDuration = _thinkingRemainingTime = thinkingDuration;
        _answeringDuration = _answeringRemainingTime = answeringDuration;
        _validatingDuration = _validatingRemainingTime = validatingDuration;
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

    public void StartThinking()
    {
        _state = EState.Thinking;
    }

    public void StartAnswering(float progress)
    {
        _state = EState.Answering;
        if (IsPlayer)
        {
            string answerID = ActiveAnswerID;
            _answerSheetUI.ShowProgress(answerID, progress);
            _answerSheetUI.Show();
        }
    }

    public void StartValidating()
    {
        _state = EState.Validating;
    }

    public void StartIdle()
    {
        _state = EState.Idle;
    }

    public float GetAnsweringDuration()
    {
        return AnswerSheet.GetAnsweringDuration(ActiveAnswerID);
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        string answerID = ActiveAnswerID;
        float progress = AnswerSheet.UpdateProgress(answerID, out finishedAnswering);
        if (IsPlayer)
        {
            _answerSheetUI.SetProgress(answerID, progress);
        }
        if (finishedAnswering)
        {
            if (IsPlayer)
            {
                _answerSheetUI.SetAnswerState(answerID, true);
                _answerSheetUI.HideProgress(answerID);
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
            string answerID = ActiveAnswerID;
            if (IsPlayer)
            {
                _answerSheetUI.HideProgress(answerID);
            }
            ActiveAnswerID = null;
            AnswerSheet.OnStopAnswering(answerID);
        }
    }

    public void TriggerFinishedPeeking()
    {
        string answerID = IsThinking || IsAnswering ? ActiveAnswerID : LastFinishedAnswerID;
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

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
    [SerializeField] private SemicircleAnswerSheetUI _semicircleAnswerSheetUI;
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private TestPageView _testPageView;

    private int _cheatBlockCount;
    private EState _state;
    private string[] _activeAnswerContributorIDs = new string[0];
    private bool HasAnswerSheet => AnswerSheet != null;
    private float _thinkingDuration;
    private float _thinkingRemainingTime;
    private float _answeringDuration;
    private float _answeringRemainingTime;
    private float _validatingDuration;
    private float _validatingRemainingTime;
    private IAnswerSheetUI _activeUI;


    public AnswerSheet AnswerSheet { get; private set; }
    public string ActiveAnswerID { get; private set; }
    public float ActiveAnswerCorrectness { get; private set; }
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
    public TestPageView TestPageView => _testPageView;

    public event Action<AnswerController, string> OnFinishPeekingEvent;
    public event Action<AnswerController, string> OnFinishAnsweringEvent;

    private void Awake()
    {
        _answerSheetUI.Hide();
        _semicircleAnswerSheetUI.Hide();
    }

    public void Setup(AnswerSheet answerSheet, string actorID, bool isPlayer)
    {
        AnswerSheet = answerSheet;
        ActorID = actorID;
        IsPlayer = isPlayer;

        if (_globalDefinition.AnswerSheetMode == GlobalDefinition.EAnswerSheetMode.SemiCircle)
            _activeUI = _semicircleAnswerSheetUI;
        else
            _activeUI = _answerSheetUI;

        if (_activeUI != null && IsPlayer)
        {
            _activeUI.Setup(answerSheet.Answers);
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

        if (HasAnswerSheet && !string.IsNullOrEmpty(ActiveAnswerID))
            AnswerSheet.SetAnsweringDuration(ActiveAnswerID, answeringDuration);
    }

    public void SetCorrectness(string answerID, float value)
    {
        if (!HasAnswerSheet || !AnswerSheet.HasAnswer(answerID)) return; // No answer sheet in this desk
        AnswerSheet.SetCorrectness(answerID, value);
    }

    public void ShowOrLiftAnswerSheet()
    {
        if (!HasAnswerSheet || !IsPlayer) return;
        _activeUI.Show();
        _testPageView?.Lift();
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

    public bool CanStartAnswering(string answerID, float correctness, string contributorActorID, string sourceID, out float progress)
    {
        if (!HasAnswerSheet || !AnswerSheet.HasAnswer(answerID))
        {
            progress = 0;
            return false; // No answer sheet in this desk
        }
        if (AnswerSheet.IsAnswerFull(answerID, out progress, out float oldCorrectness) && oldCorrectness == 1) return false; // Already answered correctly
        //if (!string.IsNullOrEmpty(contributorActorID) && AnswerSheet.HasContributor(answerID, contributorActorID)) return false; // Same source already contributed
        if (!string.IsNullOrEmpty(sourceID) && AnswerSheet.HasContributor(answerID, sourceID)) return false; // Same source 
        return true;
    }

    public bool TryStartAnswering(string answerID, float correctness, string contributorActorID, string sourceID)
    {
        if (!CanStartAnswering(answerID, correctness, contributorActorID, sourceID, out float progress)) return false;
        ActiveAnswerID = answerID;
        ActiveAnswerCorrectness = correctness;
        _activeAnswerContributorIDs = new string[] { contributorActorID, sourceID };
        if (progress >= 1)
        {
            // Answer was previously filled; reset so the progress bar can be filled again.
            AnswerSheet.ResetProgress(answerID);
            progress = 0;
        }
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
            _activeUI.ShowProgress(answerID, progress);
            _activeUI.Show();
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

    public float GetCorrectness(string answerID)
    {
        return AnswerSheet.GetCorrectness(answerID);
    }

    public float GetAnsweringDuration()
    {
        return AnswerSheet.GetAnsweringDuration(ActiveAnswerID);
    }

    public void AddContributor(string answerID, string contributorID)
    {
        AnswerSheet.AddContributor(answerID, contributorID);
    }

    public void UpdateAnswering(float deltaTime, out bool finishedAnswering)
    {
        string answerID = ActiveAnswerID;
        if (string.IsNullOrWhiteSpace(answerID))
        {
            Debug.LogError("UpdateAnswering failed. Answer ID is invalid"); // [AKP] Is there valid cases where this is expected? If so, reduce this to Warning.
            finishedAnswering = false;
            return;
        }

        float progress = AnswerSheet.UpdateProgress(answerID, deltaTime, out finishedAnswering);
        if (IsPlayer)
        {
            _activeUI.SetProgress(answerID, progress);
        }
        if (finishedAnswering)
        {
            if (IsPlayer)
            {
                float correctness = Mathf.Clamp01(ActiveAnswerCorrectness + AnswerSheet.GetCorrectness(answerID));
                AnswerSheet.SetCorrectness(answerID, correctness);
                foreach (var contributor in _activeAnswerContributorIDs)
                {
                    if (contributor == null) continue;
                    AnswerSheet.AddContributor(answerID, contributor);
                }
                _activeUI.SetAnswerState(answerID, true);
                _activeUI.SetCorrectness(answerID, correctness);
                _activeUI.HideProgress(answerID);
            }
            _activeAnswerContributorIDs = new string[0];
            LastFinishedAnswerID = answerID;
            ActiveAnswerID = null;
            OnFinishAnsweringEvent?.Invoke(this, answerID);
        }
    }

    public void HideOrLowerAnswerSheet()
    {
        if (!HasAnswerSheet) return;

        if (IsPlayer)
        {
            _activeUI.Hide();
            _testPageView?.Lower();
        }
    }

    public void StopAnswering()
    {
        if (IsAnswering)
        {
            string answerID = ActiveAnswerID;
            if (IsPlayer)
            {
                _activeUI.HideProgress(answerID);
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
            _activeUI.HideProgress();
            _activeUI.ResetAnswerStates();
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

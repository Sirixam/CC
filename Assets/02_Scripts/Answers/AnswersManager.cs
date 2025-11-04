using System;
using UnityEngine;

public class AnswerSheet
{
    [Serializable]
    public class Data
    {
        public int AnswersCount;
        public float AnswerDuration;
        public bool PersistProgress;
    }

    private Data _data;
    private float[] _answersProgress;

    public int AnswersCount => _answersProgress.Length;

    public AnswerSheet(Data data)
    {
        _data = data;
        _answersProgress = new float[_data.AnswersCount];
    }

    public float UpdateProgress(int answerIndex, out bool finishedAnswering)
    {
        float progressDelta = Time.deltaTime / _data.AnswerDuration;
        _answersProgress[answerIndex] = Mathf.Clamp01(_answersProgress[answerIndex] + progressDelta);
        finishedAnswering = _answersProgress[answerIndex] >= 1;
        return _answersProgress[answerIndex];
    }

    public int GetFullAnswersCount()
    {
        return Array.FindAll(_answersProgress, x => x >= 1).Length;
    }

    public bool IsAnswerFull(int answerIndex, out float progress)
    {
        progress = _answersProgress[answerIndex];
        return progress >= 1;
    }

    public void OnStopAnswering(int answerIndex)
    {
        if (!_data.PersistProgress)
        {
            _answersProgress[answerIndex] = 0;
        }
    }
}

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private AnswerSheet.Data _data = new() { AnswersCount = 10, AnswerDuration = 1.5f };
    [SerializeField] private DeskController[] _playerDesks;
    [SerializeField] private DeskController[] _desksWithAnswersSheet;
    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private bool _canUseAnyPlayerChair;

    private AnswerSheet[] _answerSheets;

    public event Action<int> OnAllPlayersAnsweredFullyEvent;

    public static AnswersManager GetInstance() => FindObjectOfType<AnswersManager>(); // TODO: Remove

    private void Awake()
    {
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(false);
        }
        _answerSheets = new AnswerSheet[_playerDesks.Length];
        for (int i = 0; i < _playerDesks.Length; i++)
        {
            int playerIndex = i;
            _playerDesks[i].OnFinishAnsweringEvent += OnFinishAnswering;
            _answerSheets[i] = new AnswerSheet(_data);
            _playerDesks[i].Setup(_answerSheets[i], playerIndex, _canUseAnyPlayerChair);
        }
        foreach (var deskController in _desksWithAnswersSheet)
        {
            deskController.OnFinishAnsweringEvent += OnFinishAnswering;
            deskController.Setup(null, playerIndex: -1, false);
        }
    }

    private void OnFinishAnswering(DeskController deskController, int answerNumber)
    {
        if (!deskController.IsPlayerDesk) return;

        if (HaveAllPlayersAnsweredFully(answerNumber))
        {
            OnAllPlayersAnsweredFullyEvent?.Invoke(answerNumber);
        }

        if (HaveAllPlayersAnsweredFully())
        {
            _timeManager.Pause();
            if (_victoryFeedback != null)
            {
                _victoryFeedback.SetActive(true);
            }
        }
    }

    private bool HaveAllPlayersAnsweredFully()
    {
        foreach (var answerSheet in _answerSheets)
        {
            if (answerSheet.GetFullAnswersCount() < _data.AnswersCount)
            {
                return false;
            }
        }
        return true;
    }

    public bool HaveAllPlayersAnsweredFully(int answerNumber)
    {
        foreach (var answerSheet in _answerSheets)
        {
            if (!answerSheet.IsAnswerFull(answerNumber - 1, out _))
            {
                return false;
            }
        }
        return true;
    }
}

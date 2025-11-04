using System;
using UnityEngine;

public class Answer
{
    [Serializable]
    public class Data
    {
        public Sprite TypeIcon;
        public float AnswerDuration;
    }

    private Data _data;

    public float Progress { get; private set; }
    public bool IsAnswerFull => Progress >= 1;
    public Sprite TypeIcon => _data.TypeIcon;

    public Answer(Data data)
    {
        _data = data;
    }

    public float UpdateProgress(out bool finishedAnswering)
    {
        float progressDelta = Time.deltaTime / _data.AnswerDuration;
        Progress = Mathf.Clamp01(Progress + progressDelta);
        finishedAnswering = Progress >= 1;
        return Progress;
    }

    public void ResetProgress()
    {
        Progress = 0;
    }
}

public class AnswerSheet
{
    private bool _persistProgress;

    public Answer[] Answers { get; private set; }

    public AnswerSheet(Answer.Data[] answersData, bool persistProgress)
    {
        _persistProgress = persistProgress;
        Answers = new Answer[answersData.Length];
        for (int i = 0; i < Answers.Length; i++)
        {
            Answers[i] = new Answer(answersData[i]);
        }
    }

    public float UpdateProgress(int answerIndex, out bool finishedAnswering)
    {
        return Answers[answerIndex].UpdateProgress(out finishedAnswering);
    }

    public int GetFullAnswersCount()
    {
        return Array.FindAll(Answers, x => x.IsAnswerFull).Length;
    }

    public bool IsAnswerFull(int answerIndex, out float progress)
    {
        Answer answer = Answers[answerIndex];
        progress = answer.Progress;
        return answer.IsAnswerFull;
    }

    public void OnStopAnswering(int answerIndex)
    {
        if (!_persistProgress)
        {
            Answers[answerIndex].ResetProgress();
        }
    }
}

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private Answer.Data[] _playerAnswersData;
    [SerializeField] private Answer.Data[] _npcAnswersData;
    [SerializeField] private DeskController[] _playerDesks;
    [SerializeField] private DeskController[] _npcDesks;
    [SerializeField] private AnswerPeekUI[] _answerPeekUIs;
    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private bool _canUseAnyPlayerChair;
    [SerializeField] private bool _persistProgress;

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
            _answerSheets[i] = new AnswerSheet(_playerAnswersData, _persistProgress);
            _playerDesks[i].Setup(_answerSheets[i], playerIndex, _canUseAnyPlayerChair);
        }
        foreach (var deskController in _npcDesks)
        {
            deskController.OnFinishAnsweringEvent += OnFinishAnswering;
            deskController.Setup(null, playerIndex: -1, false);
        }
        foreach (var answerPeekUI in _answerPeekUIs)
        {
            answerPeekUI.Hide();
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
            if (answerSheet.GetFullAnswersCount() < _playerAnswersData.Length)
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

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Answer
{
    private AnswerDefinition _definition;
    private float _progressPerSecond;

    public string ID => _definition.ID;
    public float Progress { get; private set; }
    public bool IsAnswerFull => Progress >= 1;
    public Sprite Icon => _definition.Icon;
    public Color Color => _definition.Color;

    public Answer(AnswerDefinition definition)
    {
        _definition = definition;
        _progressPerSecond = 1 / _definition.BaseAnswerDuration;
    }

    public float UpdateProgress(out bool finishedAnswering)
    {
        Progress = Mathf.Clamp01(Progress + Time.deltaTime * _progressPerSecond);
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
    private Dictionary<string, Answer> _id2Answer = new();

    public Answer[] Answers { get; private set; }

    public AnswerSheet(AnswerDefinition[] answersDefinitions, bool persistProgress)
    {
        _persistProgress = persistProgress;

        Answers = new Answer[answersDefinitions.Length];
        for (int i = 0; i < Answers.Length; i++)
        {
            Answer answer = new(answersDefinitions[i]);
            _id2Answer.Add(answersDefinitions[i].ID, answer);
            Answers[i] = answer;
        }
    }

    public float UpdateProgress(string answerID, out bool finishedAnswering)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            return answer.UpdateProgress(out finishedAnswering);
        }
        Debug.LogError("UpdateProgress.AnswerID was not found: " + answerID);
        finishedAnswering = false;
        return 0;
    }

    public int GetFullAnswersCount()
    {
        return Array.FindAll(Answers, x => x.IsAnswerFull).Length;
    }

    public bool HasAnswer(string answerID)
    {
        return Array.Exists(Answers, x => x.ID == answerID);
    }

    public bool IsAnswerFull(string answerID, out float progress)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            progress = answer.Progress;
            return answer.IsAnswerFull;
        }
        Debug.LogError("IsAnswerFull.AnswerID was not found: " + answerID);
        progress = 0;
        return false;
    }

    public void OnStopAnswering(string answerID)
    {
        if (_persistProgress) return;

        ResetProgress(answerID);
    }

    public void ResetProgress(string answerID)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            answer.ResetProgress();
        }
        else
        {
            Debug.LogError("ResetProgress.AnswerID was not found: " + answerID);
        }
    }

    public void ResetProgress()
    {
        foreach (var answer in _id2Answer.Values)
        {
            answer.ResetProgress();
        }
    }
}

public class AnswerPeek
{
    public string ActorID;
    public AnswerSheet AnswerSheet;
    public string AnswerID;
    public float RemainingTime;
}

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private AnswerDefinition[] _playerAnswersDefinitions;
    [SerializeField] private AnswerDefinition[] _npcAnswersDefinitions;
    [SerializeField] private AnswerController[] _playerDesks;
    [SerializeField] private AnswerPeekUI[] _answerPeekUIs;
    [SerializeField] private GlobalDefinition _globalDefinition;

    private AnswerSheet[] _playerAnswerSheets;
    private Dictionary<string, AnswerSheet> _actorId2AnswerSheet;
    private List<AnswerController> _answerControllers = new();
    private List<AnswerPeek> _activePeeks = new();

    public int RequiredPlayersCount => _playerDesks.Length;

    public event Action<string> OnAllPlayersFinishedAnswer;
    public event Action OnAllPlayersFinishedAllAnswers;

    public static AnswersManager GetInstance() => FindObjectOfType<AnswersManager>(); // TODO: Remove

    private void Awake()
    {
        _actorId2AnswerSheet = new();
        _playerAnswerSheets = new AnswerSheet[_playerDesks.Length];
        for (int i = 0; i < _playerDesks.Length; i++)
        {
            AnswerController answerController = _playerDesks[i];
            string actorID = IActor.GetPlayerID(i);
            AnswerSheet answerSheet = new(_playerAnswersDefinitions, _globalDefinition.PersistAnswerProgress);
            answerController.Setup(answerSheet, actorID, isPlayer: true);
            answerController.OnFinishAnsweringEvent += OnFinishAnswering;
            _playerAnswerSheets[i] = answerSheet;
            _actorId2AnswerSheet.Add(actorID, answerSheet);
            _answerControllers.Add(answerController);
        }
        foreach (var answerPeekUI in _answerPeekUIs)
        {
            answerPeekUI.Hide();
        }
    }

    private void Update()
    {
        for (int i = _activePeeks.Count - 1; i >= 0; i--)
        {
            AnswerPeek peek = _activePeeks[i];
            peek.RemainingTime -= Time.deltaTime;
            AnswerPeekUI answerPeekUI = Array.Find(_answerPeekUIs, x => x.AnswerPeek == peek);
            if (peek.RemainingTime > 0)
            {
                answerPeekUI.UpdateProgress();
            }
            else
            {
                answerPeekUI.Clear();
                answerPeekUI.Hide();
                _activePeeks.RemoveAt(i);
            }
        }
    }

    public void AddStudentNpc(string actorID, AnswerController answerController)
    {
        AnswerSheet answerSheet = new(_npcAnswersDefinitions, _globalDefinition.PersistAnswerProgress);
        answerController.Setup(answerSheet, actorID, isPlayer: false);
        answerController.OnFinishAnsweringEvent += OnFinishAnswering;
        answerController.OnFinishPeekingEvent += OnFinishPeeking;
        _actorId2AnswerSheet.Add(actorID, answerSheet);
        _answerControllers.Add(answerController);
    }

    public void ResetProgress()
    {
        foreach (var answerController in _answerControllers)
        {
            answerController.ResetProgress();
        }
    }

    private void OnFinishAnswering(AnswerController answerController, string answerID)
    {
        if (!answerController.IsPlayer) return;

        if (HaveAllPlayersAnsweredFully(answerID))
        {
            OnAllPlayersFinishedAnswer?.Invoke(answerID);
        }

        if (HaveAllPlayersAnsweredFully())
        {
            OnAllPlayersFinishedAllAnswers?.Invoke();
        }
    }

    private void OnFinishPeeking(AnswerController answerController, string answerID)
    {
        AnswerPeek peek = _activePeeks.Find(x => x.AnswerSheet == answerController.AnswerSheet && x.AnswerID == answerID);
        if (peek != null)
        {
            peek.RemainingTime = _globalDefinition.PeekDuration;
            return;
        }

        AnswerPeekUI peekUI = Array.Find(_answerPeekUIs, x => x.AnswerPeek == null);
        if (peekUI == null) return;

        peek = new AnswerPeek()
        {
            ActorID = answerController.ActorID,
            AnswerSheet = answerController.AnswerSheet,
            AnswerID = answerID,
            RemainingTime = _globalDefinition.PeekDuration
        };
        _activePeeks.Add(peek);

        Sprite answerTypeIcon = GetAnswerTypeIcon(answerID);
        peekUI.Setup(peek, null, answerTypeIcon);
        peekUI.Show();
    }

    public AnswerDefinition GetNewStudentAnswer(AnswerDefinition lastAnswerDef)
    {
        AnswerDefinition newAnswerDef;
        do
        {
            int answerIndex = UnityEngine.Random.Range(0, _npcAnswersDefinitions.Length);
            newAnswerDef = _npcAnswersDefinitions[answerIndex];
        } while (newAnswerDef == lastAnswerDef);
        return newAnswerDef;
    }

    public Sprite GetAnswerTypeIcon(string answerID)
    {
        AnswerDefinition answerDefinition = Array.Find(_npcAnswersDefinitions, x => x.ID == answerID);
        return answerDefinition.Icon;
    }

    private bool HaveAllPlayersAnsweredFully()
    {
        foreach (var answerSheet in _playerAnswerSheets)
        {
            if (answerSheet.GetFullAnswersCount() < _playerAnswersDefinitions.Length)
            {
                return false;
            }
        }
        return true;
    }

    public bool HaveAllPlayersAnsweredFully(string answerID)
    {
        foreach (var answerSheet in _playerAnswerSheets)
        {
            if (!answerSheet.IsAnswerFull(answerID, out _))
            {
                return false;
            }
        }
        return true;
    }

    public AnswerController GetPlayerDesk(int index)
    {
        return _playerDesks[index];
    }
}

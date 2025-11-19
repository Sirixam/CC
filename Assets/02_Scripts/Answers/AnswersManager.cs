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
    [SerializeField] private AnswerController[] _npcDesks;
    [SerializeField] private AnswerPeekUI[] _answerPeekUIs;
    [SerializeField] private GlobalDefinition _globalDefinition;

    private AnswerSheet[] _playerAnswerSheets;
    private Dictionary<string, AnswerSheet> _actorId2AnswerSheet;
    private List<AnswerPeek> _activePeeks = new();

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
        }
        for (int i = 0; i < _npcDesks.Length; i++)
        {
            AnswerController answerController = _npcDesks[i];
            string actorID = IActor.GetNpcID(i);
            AnswerSheet answerSheet = new(_npcAnswersDefinitions, _globalDefinition.PersistAnswerProgress);
            answerController.Setup(answerSheet, actorID, isPlayer: false);
            answerController.OnFinishAnsweringEvent += OnFinishAnswering;
            answerController.OnFinishPeekingEvent += OnFinishPeeking;
            _actorId2AnswerSheet.Add(actorID, answerSheet);
        }
        foreach (var answerPeekUI in _answerPeekUIs)
        {
            answerPeekUI.Hide();
        }
    }

    private void Start()
    {
        foreach (var answerController in _npcDesks)
        {
            SimulateNPCAnswering(answerController, destroyCancellationToken).Forget();
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

    private async UniTask SimulateNPCAnswering(AnswerController answerController, CancellationToken cancellationToken)
    {
        int answerIndex = UnityEngine.Random.Range(0, _npcAnswersDefinitions.Length);
        while (!cancellationToken.IsCancellationRequested)
        {
            AnswerDefinition answerDef = _npcAnswersDefinitions[answerIndex];
            bool startedThinking = answerController.TryRestartAnswering(answerDef.ID, isThinking: true);
            if (startedThinking)
            {
                await UniTask.WaitForSeconds(UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y), cancellationToken: cancellationToken);

                answerController.StartAnswering(progress: 0);

                bool finishedAnswering = false;
                while (!finishedAnswering)
                {
                    answerController.UpdateAnswering(out finishedAnswering);
                    await UniTask.Yield(cancellationToken);
                }

                await UniTask.WaitForSeconds(UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y), cancellationToken: cancellationToken);
            }

            int newAnswerIndex;
            do
            {
                newAnswerIndex = UnityEngine.Random.Range(0, _npcAnswersDefinitions.Length);
            } while (newAnswerIndex == answerIndex);
            answerIndex = newAnswerIndex;
            await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
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
}

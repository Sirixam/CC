using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Answer
{
    private AnswerDefinition _definition;

    public float Progress { get; private set; }
    public bool IsAnswerFull => Progress >= 1;
    public Sprite Icon => _definition.Icon;
    public Color Color => _definition.Color;

    public Answer(AnswerDefinition definition)
    {
        _definition = definition;
    }

    public float UpdateProgress(out bool finishedAnswering)
    {
        float progressDelta = Time.deltaTime / _definition.BaseAnswerDuration;
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

    public AnswerSheet(AnswerDefinition[] answersDefinitions, bool persistProgress)
    {
        _persistProgress = persistProgress;
        Answers = new Answer[answersDefinitions.Length];
        for (int i = 0; i < Answers.Length; i++)
        {
            Answers[i] = new Answer(answersDefinitions[i]);
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

    public bool HasAnswer(int answerIndex)
    {
        return answerIndex >= 0 && answerIndex < Answers.Length;
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

    public void ResetProgress(int answerIndex)
    {
        Answers[answerIndex].ResetProgress();
    }
}

public class AnswerPeek
{
    public string ActorID;
    public AnswerSheet AnswerSheet;
    public int AnswerNumber;
    public float RemainingTime;
}

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private AnswerDefinition[] _playerAnswersDefinitions;
    [SerializeField] private AnswerDefinition[] _npcAnswersDefinitions;
    [SerializeField] private AnswerController[] _playerDesks;
    [SerializeField] private AnswerController[] _npcDesks;
    [SerializeField] private AnswerPeekUI[] _answerPeekUIs;
    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GlobalDefinition _globalDefinition;

    private AnswerSheet[] _playerAnswerSheets;
    private Dictionary<string, AnswerSheet> _actorId2AnswerSheet;
    private List<AnswerPeek> _activePeeks = new();

    public event Action<int> OnAllPlayersAnsweredFullyEvent;

    public static AnswersManager GetInstance() => FindObjectOfType<AnswersManager>(); // TODO: Remove

    private void Awake()
    {
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(false);
        }

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
            SimulateNPCAnswering(answerController).Forget();
        }

        //yield return new WaitForSeconds(1f);

        //_answerPeekUIs[0].Setup(null, null, 0);
        //_answerPeekUIs[0].Show();

        //while (true)
        //{
        //    _answerPeekUIs[0].SetProgress(Mathf.Clamp01((Time.time - 1f) / 5));
        //    if (Time.time > 6 && !_isReady)
        //    {
        //        _isReady = true;
        //        _answerPeekUIs[0].ShowReady();
        //    }
        //    yield return null;
        //}
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

    private async UniTask SimulateNPCAnswering(AnswerController answerController)
    {
        int answerNumber = UnityEngine.Random.Range(1, _npcAnswersDefinitions.Length);
        while (true)
        {
            bool startedThinking = answerController.TryRestartAnswering(answerNumber, isThinking: true);
            if (startedThinking)
            {
                await UniTask.WaitForSeconds(UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y));

                answerController.StartAnswering(progress: 0);

                bool finishedAnswering = false;
                while (!finishedAnswering)
                {
                    answerController.UpdateAnswering(out finishedAnswering);
                    await UniTask.Yield();
                }

                await UniTask.WaitForSeconds(UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y));
            }

            int newAnswerNumber;
            do
            {
                newAnswerNumber = UnityEngine.Random.Range(1, _npcAnswersDefinitions.Length);
            } while (newAnswerNumber == answerNumber);
            answerNumber = newAnswerNumber;
            await UniTask.Yield(); // Prevent blocking if failed to start answering.
        }
    }

    private void OnFinishAnswering(AnswerController answerController, int answerNumber)
    {
        if (!answerController.IsPlayer) return;

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

    private void OnFinishPeeking(AnswerController answerController, int answerNumber)
    {
        AnswerPeek peek = _activePeeks.Find(x => x.AnswerSheet == answerController.AnswerSheet && x.AnswerNumber == answerNumber);
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
            AnswerNumber = answerNumber,
            RemainingTime = _globalDefinition.PeekDuration
        };
        _activePeeks.Add(peek);
        peekUI.Setup(peek, null, null);
        peekUI.Show();
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

    public bool HaveAllPlayersAnsweredFully(int answerNumber)
    {
        foreach (var answerSheet in _playerAnswerSheets)
        {
            if (!answerSheet.IsAnswerFull(answerNumber - 1, out _))
            {
                return false;
            }
        }
        return true;
    }
}

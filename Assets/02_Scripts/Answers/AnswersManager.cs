using System;
using System.Collections.Generic;
using UnityEngine;

public class Answer
{
    private AnswerDefinition _definition;
    private float _progressPerSecond;

    public string ID => _definition.ID;
    public float Progress { get; private set; }
    public float Correctness { get; private set; }
    public bool IsAnswerFull => Progress >= 1;
    public Sprite Icon => _definition.Icon;
    public Color Color => _definition.Color;

    public float AnswerDuration => 1 / _progressPerSecond;

    public Answer(AnswerDefinition definition)
    {
        _definition = definition;
        _progressPerSecond = 1 / _definition.BaseAnswerDuration;
    }

    public void SetCorrectness(float value)
    {
        Correctness = value;
    }

    public float UpdateProgress(float deltaTime, out bool finishedAnswering)
    {
        bool wasFull = Progress >= 1;
        Progress = Mathf.Clamp01(Progress + deltaTime * _progressPerSecond);
        finishedAnswering = !wasFull && Progress >= 1;
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

    public float GetCorrectness(string answerID)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            return answer.Correctness;
        }
        Debug.LogError("GetCorrectness.AnswerID was not found: " + answerID);
        return 0;
    }

    public float GetAnsweringDuration(string answerID)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            return answer.AnswerDuration;
        }
        Debug.LogError("GetAnsweringDuration.AnswerID was not found: " + answerID);
        return 0;
    }
    
    /* COMMENTED FOR TESTING. USE IN CASE WE WANT TO GIVE EACH ANSWER A PARTICULAR DURATION
    public float GetAnsweringDuration(string answerID)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            return answer.AnswerDuration;
        }
        Debug.LogError("GetAnsweringDuration.AnswerID was not found: " + answerID);
        return 0;
    }
    */

    public void SetCorrectness(string answerID, float value)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            //Debug.Log("SetCorrectness, answerID: " + answerID + ", value: " + value);
            answer.SetCorrectness(value);
            return;
        }
        Debug.LogError("SetCorrectness.AnswerID was not found: " + answerID);
    }

    public float UpdateProgress(string answerID, float deltaTime, out bool finishedAnswering)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            return answer.UpdateProgress(deltaTime, out finishedAnswering);
        }
        Debug.LogError("UpdateProgress.AnswerID was not found: " + answerID);
        finishedAnswering = false;
        return 0;
    }

    public int GetFullAnswersCount(out float minCorrectness)
    {
        minCorrectness = float.MaxValue;
        int fullAnswersCount = 0;
        foreach (var answer in Answers)
        {
            if (answer.IsAnswerFull)
            {
                minCorrectness = Mathf.Min(minCorrectness, answer.Correctness);
                fullAnswersCount++;
            }
        }
        return fullAnswersCount;
    }

    public bool HasAnswer(string answerID)
    {
        return Array.Exists(Answers, x => x.ID == answerID);
    }

    public bool IsAnswerFull(string answerID, out float progress, out float correctness)
    {
        if (_id2Answer.TryGetValue(answerID, out Answer answer))
        {
            progress = answer.Progress;
            correctness = answer.Correctness;
            return answer.IsAnswerFull;
        }
        Debug.LogError("IsAnswerFull.AnswerID was not found: " + answerID);
        progress = 0;
        correctness = 0;
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
    public AnswerController AnswerController;
    public string AnswerID;
    public float ShowRemainingTime;
    public AnswerPeekUI PeekUI;

    public bool FinishedValidating => AnswerController.ValidatingPercent >= 1f;
}

public class AnswersManager : MonoBehaviour, IAnswerIconProvider
{
    public struct StudentNpcInput
    {
        public string ActorID;
        public AnswerController AnswerController;
        public Sprite CharacterIcon;
        public Sprite ArchetypeIcon;
    }

    [SerializeField] private AnswerDefinition[] _playerAnswersDefinitions;
    [SerializeField] private AnswerDefinition[] _npcAnswersDefinitions;
    [SerializeField] private AnswerController[] _playerDesks;
    //[SerializeField] private AnswerPeekUI[] _answerPeekUIs;
    [SerializeField] private AnswerPeekUI _answerPeekUI;
    [SerializeField] private Transform _peekUIParent;
    [SerializeField] private int _maxPeekUIs = 5;
    private Queue<AnswerPeekUI> _activePeekUIs = new();

    [SerializeField] private GlobalDefinition _globalDefinition;

    private TestDefinition _testDefinition;
    public AnswerSheet[] PlayerAnswerSheets { get; private set; }
    private Dictionary<string, AnswerSheet> _actorId2AnswerSheet;
    private List<AnswerController> _answerControllers = new();
    private List<AnswerPeek> _activePeeks = new();

    public int RequiredPlayersCount => _playerDesks.Length;

    public event Action<string, float> OnAllPlayersFinishedAnswer; // Params: float minCorrectness
    public event Action<float> OnAllPlayersFinishedAllAnswers; // Params: float minCorrectness

    private void Awake()
    {
        _actorId2AnswerSheet = new();
        PlayerAnswerSheets = new AnswerSheet[_playerDesks.Length];
        for (int i = 0; i < _playerDesks.Length; i++)
        {
            AnswerController answerController = _playerDesks[i];
            string actorID = IActor.GetPlayerID(i);
            AnswerSheet answerSheet = new(_playerAnswersDefinitions, _globalDefinition.PersistAnswerProgress);
            answerController.Setup(answerSheet, actorID, isPlayer: true);
            answerController.OnFinishAnsweringEvent += OnFinishAnswering;
            PlayerAnswerSheets[i] = answerSheet;
            _actorId2AnswerSheet.Add(actorID, answerSheet);
            _answerControllers.Add(answerController);
        }
        /*foreach (var answerPeekUI in _answerPeekUI)
        {
            answerPeekUI.Hide();
        }*/
        if (_answerPeekUI == null)
        {
            Debug.LogError("Peek UI Prefab is not assigned!");
        }
    }

    private void Update()
    {
        for (int i = _activePeeks.Count - 1; i >= 0; i--)
        {
            AnswerPeek peek = _activePeeks[i];
            AnswerPeekUI answerPeekUI = peek.PeekUI;
            //AnswerPeekUI answerPeekUI = Array.Find(_answerPeekUIs, x => x.AnswerPeek == peek);
            if (!peek.FinishedValidating)
            {
                answerPeekUI.UpdateProgress(setup: false);
            }
            else
            {
                answerPeekUI.Clear();
                answerPeekUI.Hide();
                _activePeeks.RemoveAt(i);
            }
        }
    }

    public void InjectTestDefinition(TestDefinition testDefinition)
    {
        _testDefinition = testDefinition;
    }

    public void AddStudentNpc(StudentNpcInput input)
    {
        AnswerDefinition[] answerDefinitions = GetAnswerDefinitions();
        AnswerSheet answerSheet = new(answerDefinitions, _globalDefinition.PersistAnswerProgress);
        input.AnswerController.Setup(answerSheet, input.ActorID, isPlayer: false);
        input.AnswerController.OnFinishAnsweringEvent += OnFinishAnswering;
        input.AnswerController.OnFinishPeekingEvent += (x, y) => OnFinishPeeking(x, y, input.CharacterIcon, input.ArchetypeIcon);
        _actorId2AnswerSheet.Add(input.ActorID, answerSheet);
        _answerControllers.Add(input.AnswerController);
    }

    private AnswerDefinition[] GetAnswerDefinitions()
    {
#if UNITY_EDITOR
        if (_testDefinition != null && _testDefinition.ForcedNpcAnswer != null)
        {
            return new AnswerDefinition[] { _testDefinition.ForcedNpcAnswer };
        }
#endif
        return _npcAnswersDefinitions;
    }

    public void CleanActivePeeks()
    {
        while (_activePeekUIs.Count > 0)
        {
            AnswerPeekUI ui = _activePeekUIs.Dequeue();
            ui.Clear();
            Destroy(ui.gameObject);
        }

        _activePeeks.Clear();
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

        if (HaveAllPlayersAnsweredFully(answerID, out float answerMinCorrectness))
        {
            OnAllPlayersFinishedAnswer?.Invoke(answerID, answerMinCorrectness);
        }

        if (HaveAllPlayersAnsweredFully(out float minCorrectness))
        {
            OnAllPlayersFinishedAllAnswers?.Invoke(minCorrectness);
        }
    }

    private void OnFinishPeeking(AnswerController answerController, string answerID, Sprite characterIcon, Sprite archetypeIcon)
    {
        AnswerPeek peek = _activePeeks.Find(x => x.AnswerSheet == answerController.AnswerSheet && x.AnswerID == answerID);
        if (peek != null) return; // Already showing this peek

        //AnswerPeekUI peekUI = Array.Find(_answerPeekUIs, x => x.AnswerPeek == null);
        //if (peekUI == null) return; // No available peek UI, TODO: Replace one of them.
        if (_activePeekUIs.Count >= _maxPeekUIs)
        {
            AnswerPeekUI oldest = _activePeekUIs.Dequeue();
            oldest.Clear();
            Destroy(oldest.gameObject);
        }

        AnswerPeekUI peekUI = Instantiate(_answerPeekUI, _peekUIParent);
        _activePeekUIs.Enqueue(peekUI);

        peek = new AnswerPeek()
        {
            ActorID = answerController.ActorID,
            AnswerController = answerController,
            AnswerSheet = answerController.AnswerSheet,
            AnswerID = answerID,
        };
        peek.PeekUI = peekUI;

        _activePeeks.Add(peek);

        Sprite answerTypeIcon = GetAnswerTypeIcon(answerID);
        peekUI.Setup(peek, characterIcon, archetypeIcon, answerTypeIcon);
        peekUI.Show();
    }

    public AnswerDefinition GetNewStudentAnswer(AnswerDefinition lastAnswerDef)
    {
#if UNITY_EDITOR
        if (_testDefinition != null && _testDefinition.ForcedNpcAnswer != null)
        {
            return _testDefinition.ForcedNpcAnswer;
        }
#endif
        if (_npcAnswersDefinitions.Length == 0)
        {
            Debug.LogError("No NPC answer definitions found!");
            return null;
        }

        if (_npcAnswersDefinitions.Length == 1) return _npcAnswersDefinitions[0];

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

    private bool HaveAllPlayersAnsweredFully(out float minCorrectness)
    {
        minCorrectness = float.MaxValue;
        foreach (var answerSheet in PlayerAnswerSheets)
        {
            if (answerSheet.GetFullAnswersCount(out float sheetMinCorrectness) < _playerAnswersDefinitions.Length)
            {
                minCorrectness = 0;
                return false;
            }
            minCorrectness = Mathf.Min(minCorrectness, sheetMinCorrectness);
        }
        return true;
    }

    public bool HaveAllPlayersAnsweredFully(string answerID, out float minCorrectness)
    {
        minCorrectness = float.MaxValue;
        foreach (var answerSheet in PlayerAnswerSheets)
        {
            if (!answerSheet.IsAnswerFull(answerID, out _, out float correctness))
            {
                minCorrectness = 0;
                return false;
            }
            minCorrectness = Mathf.Min(minCorrectness, correctness);
        }
        return true;
    }

    public AnswerController GetPlayerDesk(int index)
    {
        return _playerDesks[index];
    }
}

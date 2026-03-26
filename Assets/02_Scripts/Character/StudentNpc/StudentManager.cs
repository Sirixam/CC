using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;


using Random = UnityEngine.Random;

public class StudentManager : MonoBehaviour
{
    private const float HALF_CORRECTNESS = 0.5f;

    [SerializeField] private AnswersManager _answerManager;
    [SerializeField] private StudentNpcController[] _students;

    [Header("Configurations")]
    [SerializeField] private bool _simulateStudentsIndividually;
    [Range(0, 1f), Tooltip("What's the chance of getting a half correct answer versus a wrong answer.")]
    [SerializeField] private float _halfCorrectChance = 0.5f;

    [Header("Phase Durations")]
    [Tooltip("Also used as fallback for thinking duration range (min/max seconds) used when no curve is set.")]
    [SerializeField] private float _peekPhaseDuration = 20f;
    [SerializeField] private float _cheatPhaseDuration = 20f;
    [SerializeField] private Vector2 _answeringDurationRange = new Vector2(5f, 10f);

    [Tooltip("X axis: student index. Y axis: thinking duration in seconds. Overrides the range above when set.")]
    [SerializeField] private AnimationCurve _thinkingDurationCurve;
    [Tooltip("When true, X axis is normalized (0 = first student, 1 = last student). When false, X axis is the raw student index.")]
    [SerializeField] private bool _normalizeStudentAxis = true;
    [Tooltip("If true, answering duration = peek phase duration - thinking duration, so students who think longer have less time to answer.")]
    [SerializeField] private bool _dynamicAnsweringDuration;

    public float PeekPhaseDuration => _peekPhaseDuration;
    public float CheatPhaseDuration => _cheatPhaseDuration;
    public float AverageAnsweringDuration => (_answeringDurationRange.x + _answeringDurationRange.y) / 2f;

    private TestDefinition _testDefinition;
    private StudentNpcController _smartStudent;
    private CancellationTokenSource _simulationCancellationSource;
    private int _smartStudentStreak;
    private StudentNpcController _lastSmartStudent;
    private List<AnswerDefinition> _availableCorrectAnswers;
    private const int MAX_SMART_STREAK = 2;
    private int[] _shuffledCurveIndices;


    public Action<PlayerController> OnPlayerDetected;
    public Action<IItemController> OnItemDetected;

    private void Start()
    {
        for (int i = 0; i < _students.Length; i++)
        {
            string actorID = IActor.GetStudentNpcID(i);
            _students[i].OnPlayerDetected += OnPlayerDetected.Invoke;
            _students[i].OnItemDetected += OnItemDetected.Invoke;
            _answerManager.AddStudentNpc(new AnswersManager.StudentNpcInput
            {
                ActorID = actorID,
                AnswerController = _students[i].AnswerController,
                CharacterIcon = _students[i].CharacterIcon,
                ArchetypeIcon = _students[i].ArchetypeIcon
            });
        }
    }

    public void InjectTestDefinition(TestDefinition testDefinition)
    {
        _testDefinition = testDefinition;
        foreach (var student in _students)
        {
            student.InjectTestDefinition(testDefinition);
        }
    }

    public void StartStimulation(CancellationToken externalToken)
    {
        _simulationCancellationSource?.Cancel();
        _simulationCancellationSource?.Dispose();
        _simulationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        // Initialize the correct answer pool if empty or first time
        if (_availableCorrectAnswers == null || _availableCorrectAnswers.Count == 0)
        {
            RefillCorrectAnswerPool();
        }

        ShuffleCurveIndices();

        StudentNpcController smartStudent = GetNewSmartStudent();
        AnswerDefinition correctAnswer = GetCorrectAnswerFromPool();

        if (_simulateStudentsIndividually)
        {
            foreach (var student in _students)
            {
                SimulateNPCAnswering(student, _simulationCancellationSource.Token, smartStudent, correctAnswer).Forget();
            }
        }
        else
        {
            SimulateNPCsAnswering(_simulationCancellationSource.Token, smartStudent, correctAnswer).Forget();
        }
    }

    private float GetThinkingDuration(int studentIndex)
    {
        if (_thinkingDurationCurve == null || _thinkingDurationCurve.length == 0)
            return _peekPhaseDuration;

        int curveIndex = (_shuffledCurveIndices != null && studentIndex < _shuffledCurveIndices.Length)
            ? _shuffledCurveIndices[studentIndex]
            : studentIndex;

        float t = _normalizeStudentAxis
            ? (_students.Length > 1 ? (float)curveIndex / (_students.Length - 1) : 0f)
            : curveIndex;
        return _thinkingDurationCurve.Evaluate(t);
    }

    private float GetAnsweringDuration(float thinkingDuration)
    {
        if (_dynamicAnsweringDuration)
            return Mathf.Max(0f, _peekPhaseDuration - thinkingDuration);
        return Random.Range(_answeringDurationRange.x, _answeringDurationRange.y);
    }

    private void ShuffleCurveIndices()
    {
        _shuffledCurveIndices = new int[_students.Length];
        for (int i = 0; i < _shuffledCurveIndices.Length; i++)
            _shuffledCurveIndices[i] = i;

        for (int i = _shuffledCurveIndices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_shuffledCurveIndices[i], _shuffledCurveIndices[j]) = (_shuffledCurveIndices[j], _shuffledCurveIndices[i]);
        }
    }

    private void RefillCorrectAnswerPool()
    {
        _availableCorrectAnswers = new List<AnswerDefinition>(_answerManager.GetNpcAnswerDefinitions());
    }

    private async UniTask SimulateNPCAnswering(StudentNpcController student, CancellationToken cancellationToken, StudentNpcController smartStudent, AnswerDefinition correctAnswer)
    {
        bool isSmartStudent = student == smartStudent;
        GetAnswerData(isSmartStudent, correctAnswer, out AnswerDefinition answerDef, out float correctness);

        string answerID = answerDef.ID;
        bool startedThinking = student.AnswerController.TryRestartAnswering(answerID, isThinking: true);
        if (startedThinking)
        {
            student.SetCorrectness(answerID, correctness);
            int studentIndex = Array.IndexOf(_students, student);
            float thinkingDuration = GetThinkingDuration(studentIndex);
            student.SetDurations(thinkingDuration,
                                    answeringDuration: GetAnsweringDuration(thinkingDuration),
                                    validatingDuration: _cheatPhaseDuration);

            student.StartThinking();
            await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);

            student.StartAnswering();
            await student.UpdateAnsweringTaskWhileNotDistracted(cancellationToken: cancellationToken);

            student.StartValidating();
            await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);
        }

        await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
    }

    private async UniTask SimulateNPCsAnswering(CancellationToken cancellationToken, StudentNpcController smartStudent, AnswerDefinition correctAnswer)
    {
        float sharedAnsweringDuration = Random.Range(_answeringDurationRange.x, _answeringDurationRange.y);
        float validatingDuration = _cheatPhaseDuration;

        for (int i = 0; i < _students.Length; i++)
        {
            StudentNpcController student = _students[i];
            bool isSmartStudent = student == smartStudent;
            GetAnswerData(isSmartStudent, correctAnswer, out AnswerDefinition answerDef, out float correctness);

            string answerID = answerDef.ID;
            bool startedThinking = student.AnswerController.TryRestartAnswering(answerID, isThinking: true);
            if (startedThinking)
            {
                student.SetCorrectness(answerID, correctness);
                float thinkingDuration = GetThinkingDuration(i);
                float answeringDuration = _dynamicAnsweringDuration ? GetAnsweringDuration(thinkingDuration) : sharedAnsweringDuration;
                student.SetDurations(thinkingDuration, answeringDuration, validatingDuration);
                student.StartThinking();
            }
        }

        await UpdateRemainingTimeOnAllStudents(cancellationToken);

        foreach (var student in _students)
        {
            student.StartAnswering();
        }

        await UpdateAnsweringOnAllStudents(cancellationToken);

        foreach (var student in _students)
        {
            student.StartValidating();
        }

        await UpdateRemainingTimeOnAllStudents(cancellationToken);
    }

    private void GetAnswerData(bool isSmartStudent, AnswerDefinition correctAnswer, out AnswerDefinition answerDef, out float correctness)
    {
        if (isSmartStudent)
        {
            // Clever student always answers the correct answer from the pool
            answerDef = correctAnswer;
            correctness = 1f;
        }
        else
        {
            // Other students answer the same question but with half/wrong correctness
            answerDef = _answerManager.GetNewStudentAnswer(correctAnswer);
            correctness = GetNewCorrectness(isSmartStudent: false);
        }
    }

    private float GetNewCorrectness(bool isSmartStudent)
    {
#if UNITY_EDITOR
        if (_testDefinition != null)
        {
            if (_testDefinition.ForcedCorrectAnswer) return 1;
            if (_testDefinition.ForcedHalfCorrectAnswer) return HALF_CORRECTNESS;
            if (_testDefinition.ForcedWrongAnswer) return 0;
        }
#endif

        if (isSmartStudent) return 1;

        bool isHalfCorrect = Random.Range(0, 1f) <= _halfCorrectChance;
        return isHalfCorrect ? HALF_CORRECTNESS : 0;
    }

    private StudentNpcController GetNewSmartStudent()
    {
        if (_students.Length <= 1)
        {
            _lastSmartStudent = _students[0];
            return _students[0];
        }

        StudentNpcController candidate;
        do
        {
            int index = Random.Range(0, _students.Length);
            candidate = _students[index];
        } while (candidate == _lastSmartStudent && _smartStudentStreak >= MAX_SMART_STREAK);

        if (candidate == _lastSmartStudent)
        {
            _smartStudentStreak++;
        }
        else
        {
            _smartStudentStreak = 1;
            _lastSmartStudent = candidate;
        }

        return candidate;
    }

    private async UniTask UpdateRemainingTimeOnAllStudents(CancellationToken cancellationToken)
    {
        bool finished = false;
        while (!finished && !cancellationToken.IsCancellationRequested)
        {
            finished = true;
            foreach (var student in _students)
            {
                if (string.IsNullOrEmpty(student.AnswerController.ActiveAnswerID)) continue;

                student.AnswerController.UpdateRemainingTime(Time.deltaTime, out bool studentFinished);
                if (!studentFinished) finished = false;
            }
            await UniTask.Yield(cancellationToken);
        }
    }

    private async UniTask UpdateAnsweringOnAllStudents(CancellationToken cancellationToken)
    {
        bool finished = false;
        while (!finished && !cancellationToken.IsCancellationRequested)
        {
            finished = true;
            foreach (var student in _students)
            {
                if (string.IsNullOrEmpty(student.AnswerController.ActiveAnswerID)) continue;

                student.AnswerController.UpdateAnswering(Time.deltaTime, out bool studentFinished);
                if (!studentFinished) finished = false;
            }
            await UniTask.Yield(cancellationToken);
        }
    }

    public void RestartStimulation()
    {
        StartStimulation(CancellationToken.None);
    }

    private AnswerDefinition GetCorrectAnswerFromPool()
    {
        if (_availableCorrectAnswers.Count == 0)
        {
            RefillCorrectAnswerPool();
        }

        int index = Random.Range(0, _availableCorrectAnswers.Count);
        AnswerDefinition answer = _availableCorrectAnswers[index];
        _availableCorrectAnswers.RemoveAt(index);
        return answer;
    }

    public void ResetForNewGame()
    {
        _availableCorrectAnswers = null;
        _smartStudentStreak = 0;
        _lastSmartStudent = null;
    }
}

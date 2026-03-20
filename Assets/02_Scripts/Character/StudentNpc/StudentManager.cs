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
    [SerializeField] private GlobalDefinition _globalDefinition;

    [Header("Configurations")]
    [Range(0, 1f), Tooltip("What's the chance of getting a half correct answer versus a wrong answer.")]
    [SerializeField] private float _halfCorrectChance = 0.5f;

    private TestDefinition _testDefinition;
    private StudentNpcController _smartStudent;
    private CancellationTokenSource _simulationCancellationSource;
    private int _smartStudentStreak;
    private StudentNpcController _lastSmartStudent;
    private List<AnswerDefinition> _availableCorrectAnswers;
    private const int MAX_SMART_STREAK = 2;


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

        if (_globalDefinition.SimulateStudentsIndividually)
        {
            foreach (var student in _students)
                SimulateNPCAnswering(student, _simulationCancellationSource.Token).Forget();
        }
        else
        {
            SimulateNPCsAnswering(_simulationCancellationSource.Token).Forget();
        }
    }

    private void RefillCorrectAnswerPool()
    {
        _availableCorrectAnswers = new List<AnswerDefinition>(_answerManager.GetNpcAnswerDefinitions());
    }

    private async UniTask SimulateNPCAnswering(StudentNpcController student, CancellationToken cancellationToken)
    {
        AnswerDefinition answerDef = null;
            answerDef = _answerManager.GetNewStudentAnswer(answerDef);
            string answerID = answerDef.ID;
            bool startedThinking = student.AnswerController.TryRestartAnswering(answerID, isThinking: true);
            if (startedThinking)
            {
                student.SetCorrectness(answerID, 1); // TODO: Dynamic correctness algorithm.
                student.SetDurations(thinkingDuration: Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y),
                                        answeringDuration: Random.Range(_globalDefinition.AnsweringDuration.x, _globalDefinition.AnsweringDuration.y),
                                        validatingDuration: Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y));

                student.StartThinking();
                await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);

                student.StartAnswering();
                await student.UpdateAnsweringTaskWhileNotDistracted(cancellationToken: cancellationToken);

                student.StartValidating();
                await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);
            }

            await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
    }

    private async UniTask SimulateNPCsAnswering(CancellationToken cancellationToken)
    {
        float thinkingDuration = Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y);
        float answeringDuration = Random.Range(_globalDefinition.AnsweringDuration.x, _globalDefinition.AnsweringDuration.y);
        float validatingDuration = Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y);

        StudentNpcController smartStudent = GetNewSmartStudent();
        AnswerDefinition correctAnswer = GetCorrectAnswerFromPool();

        foreach (var student in _students)
        {
            AnswerDefinition answerDef;
            float correctness;

            if (student == smartStudent)
            {
                // Clever student always answers the correct answer from the pool
                answerDef = correctAnswer;
                correctness = 1f;
            }
            else
            {
                // Other students answer the same question but with half/wrong correctness
                answerDef = _answerManager.GetNewStudentAnswer(correctAnswer);
                correctness = GetNewCorrectness(false);
            }

            string answerID = answerDef.ID;
            bool startedThinking = student.AnswerController.TryRestartAnswering(answerID, isThinking: true);
            if (startedThinking)
            {
                student.SetCorrectness(answerID, correctness);
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
            foreach (var student in _students)
            {
                student.AnswerController.UpdateRemainingTime(Time.deltaTime, out finished);
            }
            await UniTask.Yield(cancellationToken);
        }
    }

    private async UniTask UpdateAnsweringOnAllStudents(CancellationToken cancellationToken)
    {
        bool finished = false;
        while (!finished && !cancellationToken.IsCancellationRequested)
        {
            foreach (var student in _students)
            {
                student.AnswerController.UpdateAnswering(Time.deltaTime, out finished);
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

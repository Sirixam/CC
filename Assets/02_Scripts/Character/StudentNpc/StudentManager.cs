using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

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

    public void StartStimulation(CancellationToken cancellationToken)
    {
        if (_globalDefinition.SimulateStudentsIndividually)
        {
            foreach (var student in _students)
            {
                SimulateNPCAnswering(student, cancellationToken).Forget();
            }
        }
        else
        {
            SimulateNPCsAnswering(cancellationToken).Forget();
        }
    }

    private async UniTask SimulateNPCAnswering(StudentNpcController student, CancellationToken cancellationToken)
    {
        AnswerDefinition answerDef = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            answerDef = _answerManager.GetNewStudentAnswer(answerDef);
            string answerID = answerDef.ID;
            bool startedThinking = student.AnswerController.TryRestartAnswering(answerID, isThinking: true);
            if (startedThinking)
            {
                student.SetCorrectness(answerID, 1); // TODO: Dynamic correctness algorithm.
                student.SetDurations(thinkingDuration: Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y),
                                        validatingDuration: Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y));

                student.StartThinking();
                await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);

                student.StartAnswering();
                await student.UpdateAnsweringTaskWhileNotDistracted(cancellationToken);

                student.StartValidating();
                await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);
            }

            await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
        }
    }

    private async UniTask SimulateNPCsAnswering(CancellationToken cancellationToken)
    {
        AnswerDefinition answerDef = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            float thinkingDuration = Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y);
            float validatingDuration = Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y);

            StudentNpcController smartStudent = GetNewSmartStudent();
            foreach (var student in _students)
            {
                answerDef = _answerManager.GetNewStudentAnswer(answerDef);
                string answerID = answerDef.ID;
                bool startedThinking = student.AnswerController.TryRestartAnswering(answerID, isThinking: true);
                if (startedThinking)
                {
                    float correctness = GetNewCorrectness(student == smartStudent);
                    student.SetCorrectness(answerID, correctness);
                    student.SetDurations(thinkingDuration, validatingDuration);
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

        int index;
        do
        {
            index = Random.Range(0, _students.Length);
        } while (_smartStudent == _students[index]);
        return _students[index];
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
}

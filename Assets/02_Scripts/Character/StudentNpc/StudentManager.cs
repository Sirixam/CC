using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class StudentManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;
    [SerializeField] private StudentNpcController[] _students;
    [SerializeField] private GlobalDefinition _globalDefinition;

    public Action<PlayerController> OnPlayerDetected;
    public Action<IItemController> OnItemDetected;

    private void Start()
    {
        for (int i = 0; i < _students.Length; i++)
        {
            string actorID = IActor.GetStudentNpcID(i);
            _students[i].OnPlayerDetected += OnPlayerDetected.Invoke;
            _students[i].OnItemDetected += OnItemDetected.Invoke;
            _answerManager.AddStudentNpc(actorID, _students[i].AnswerController);
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
            bool startedThinking = student.AnswerController.TryRestartAnswering(answerDef.ID, isThinking: true);
            if (startedThinking)
            {
                student.SetDurations(thinkingDuration: UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y),
                                        validatingDuration: UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y));

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
            float thinkingDuration = UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y);
            float validatingDuration = UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y);
            foreach (var student in _students)
            {
                answerDef = _answerManager.GetNewStudentAnswer(answerDef);
                bool startedThinking = student.AnswerController.TryRestartAnswering(answerDef.ID, isThinking: true);
                if (startedThinking)
                {
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

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

    private void Start()
    {
        for (int i = 0; i < _students.Length; i++)
        {
            string actorID = IActor.GetStudentNpcID(i);
            _students[i].OnPlayerDetected += OnPlayerDetected.Invoke;
            _answerManager.AddStudentNpc(actorID, _students[i].AnswerController);
        }
    }

    public void StartStimulation(CancellationToken cancellationToken)
    {
        foreach (var student in _students)
        {
            SimulateNPCAnswering(student, cancellationToken).Forget();
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
                student.StartThinking();
                await WaitWhileNotDistracted(student, UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y), cancellationToken: cancellationToken);

                student.StartAnswering();

                bool finishedAnswering = false;
                while (!finishedAnswering && !student.IsDistracted)
                {
                    student.AnswerController.UpdateAnswering(out finishedAnswering);
                    await UniTask.Yield(cancellationToken);
                }

                student?.StartValidating();
                await WaitWhileNotDistracted(student, UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y), cancellationToken: cancellationToken);
            }

            await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
        }
    }

    private async UniTask WaitWhileNotDistracted(StudentNpcController student, float timer, CancellationToken cancellationToken)
    {
        while (timer > 0 && !cancellationToken.IsCancellationRequested)
        {
            if (!student.IsDistracted)
            {
                timer -= Time.deltaTime;
            }
            await UniTask.Yield(cancellationToken);
        }
    }
}

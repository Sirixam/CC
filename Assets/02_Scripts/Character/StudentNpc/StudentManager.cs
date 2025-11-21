using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class StudentManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;
    [SerializeField] private StudentNpcController[] _students;
    [SerializeField] private GlobalDefinition _globalDefinition;

    public void StartStimulation(CancellationToken cancellationToken)
    {
        foreach (var answerController in _answerManager.StudentNpcDesks)
        {
            SimulateNPCAnswering(answerController, cancellationToken).Forget();
        }
    }

    private async UniTask SimulateNPCAnswering(AnswerController answerController, CancellationToken cancellationToken)
    {
        StudentNpcController student = Array.Find(_students, x => x.AnswerController == answerController);
        AnswerDefinition answerDef = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            answerDef = _answerManager.GetNewStudentAnswer(answerDef);
            bool startedThinking = answerController.TryRestartAnswering(answerDef.ID, isThinking: true);
            if (startedThinking)
            {
                student?.StartThinking();
                await UniTask.WaitForSeconds(UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y), cancellationToken: cancellationToken);

                student?.StartAnswering();
                answerController.StartAnswering(progress: 0);

                bool finishedAnswering = false;
                while (!finishedAnswering)
                {
                    answerController.UpdateAnswering(out finishedAnswering);
                    await UniTask.Yield(cancellationToken);
                }

                student?.StartValidating();
                await UniTask.WaitForSeconds(UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y), cancellationToken: cancellationToken);
            }

            await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
        }
    }
}

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
                student.SetDurations(thinkingDuration: UnityEngine.Random.Range(_globalDefinition.PreAnsweringDelay.x, _globalDefinition.PreAnsweringDelay.y),
                                        validatingDuration: UnityEngine.Random.Range(_globalDefinition.PostAnsweringDelay.x, _globalDefinition.PostAnsweringDelay.y));

                student.StartThinking();
                await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);

                
                
                student.StartAnswering();
                await student.UpdateAnsweringTask(cancellationToken);
                
                

                student.StartValidating();
                await student.UpdateRemainingTimeWhileNotDistracted(cancellationToken: cancellationToken);
            }

            await UniTask.Yield(cancellationToken); // Prevent blocking if failed to start answering.
        }
    }
}

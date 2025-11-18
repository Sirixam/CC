
using UnityEngine;

public class DeskHelper
{
    private PlayerInputHandler _inputHandler;
    private ChairController _chairController;
    private AnswerController _answerController;
    private PlayerView _actorView;
    private PlayerPhysics _actorPhysics;

    public bool IsTransitioning { get; private set; }
    public bool IsSitting { get; private set; }
    public bool IsAnswering => _answerController != null && _answerController.IsAnswering;
    public bool IsCheckingAnswer => _answerController != null && _answerController.IsCheckingAnswer;

    public Transform LookAtPoint => _answerController != null ? _answerController.LookAtPoint : null;

    public DeskHelper(PlayerInputHandler inputHandler, PlayerView actorView, PlayerPhysics actorPhysics)
    {
        _inputHandler = inputHandler;
        _actorView = actorView;
        _actorPhysics = actorPhysics;
    }

    public void StartSitting(ChairController chairController)
    {
        _chairController = chairController;
        _answerController = chairController.AnswerController;
        IsTransitioning = true;
        IsSitting = true;
        _actorPhysics.OnArriveEvent -= OnArrive;
        _actorPhysics.OnArriveEvent += OnArrive;

        _chairController.Block();
        _actorPhysics.SetTargetPoint(chairController.SittingPoint);
        _inputHandler.SetScope(EInputScope.PlayerSitting);
    }

    public void StartStanding()
    {
        _answerController?.HideAnswerSheet();
        Transform standingPoint = GetBestStandingPoint(_chairController);
        _answerController = null;
        IsTransitioning = true;
        IsSitting = false;

        _chairController.Unblock();
        _actorView.OnStanding();
        _actorPhysics.OnArriveEvent -= OnArrive;
        _actorPhysics.OnArriveEvent += OnArrive;
        _actorPhysics.SetTargetPoint(standingPoint);
        _inputHandler.SetScope(EInputScope.PlayerStanding);
    }

    // TODO: Check if point is blocked.
    private Transform GetBestStandingPoint(ChairController chairController)
    {
        int bestIndex = Random.Range(0, chairController.StandingPoints.Length);
        return chairController.StandingPoints[bestIndex];
    }

    public void OnArrive()
    {
        if (IsSitting)
        {
            _actorView.OnSitting();
        }
        _actorPhysics.OnArriveEvent -= OnArrive;
        IsTransitioning = false;
        _actorPhysics.SetTargetPoint(null); // Clear
    }

    public void TryShowAnswersSheet()
    {
        if (!IsSitting) return;
        _answerController.ShowAnswerSheet();
    }

    public void TryStartAnswering(string answerID)
    {
        if (!IsSitting) return;
        _answerController.TryStartAnswering(answerID);
    }

    public void TryUpdateAnswering(out bool finishedAnswering)
    {
        if (!IsSitting)
        {
            finishedAnswering = false;
            return;
        }
        _answerController.UpdateAnswering(out finishedAnswering);
    }

    public void HideAnswersSheet()
    {
        _answerController?.HideAnswerSheet();
    }
}


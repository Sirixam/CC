
using UnityEngine;

public class DeskHelper
{
    private PlayerInputHandler _inputHandler;
    private ChairController _chairController;
    private DeskController _deskController;
    private PlayerView _actorView;
    private PlayerPhysics _actorPhysics;

    public bool IsTransitioning { get; private set; }
    public bool IsSitting { get; private set; }
    public bool IsAnswering => _deskController != null && _deskController.IsAnswering;

    public Transform LookAtPoint => _deskController != null ? _deskController.LookAtPoint : null;

    public DeskHelper(PlayerInputHandler inputHandler, PlayerView actorView, PlayerPhysics actorPhysics)
    {
        _inputHandler = inputHandler;
        _actorView = actorView;
        _actorPhysics = actorPhysics;
    }

    public void StartSitting(ChairController chairController)
    {
        _chairController = chairController;
        _deskController = chairController.DeskController;
        IsTransitioning = true;
        IsSitting = true;
        _actorPhysics.OnArriveEvent -= OnArrive;
        _actorPhysics.OnArriveEvent += OnArrive;
        _actorPhysics.SetTargetPoint(chairController.SittingPoint);
        _inputHandler.SetScope(EInputScope.PlayerSitting);
    }

    public void StartStanding()
    {
        Transform standingPoint = GetBestStandingPoint(_chairController);
        _deskController = null;
        IsTransitioning = true;
        IsSitting = false;
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
        _deskController.ShowAnswersSheet();
    }

    public void TryStartAnswering(int answerNumber)
    {
        if (!IsSitting) return;
        _deskController.TryStartAnswering(answerNumber);
    }

    public void TryUpdateAnswering(out bool finishedAnswering)
    {
        if (!IsSitting)
        {
            finishedAnswering = false;
            return;
        }
        _deskController.UpdateAnswering(out finishedAnswering);
    }

    public void HideAnswersSheet()
    {
        _deskController?.HideAnswersSheet();
    }
}


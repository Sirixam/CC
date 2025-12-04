
using UnityEngine;

public interface IChairView
{
    void OnStanding();
    void OnSitting();
}

public class ChairHelper
{
    private PlayerInputHandler _inputHandler;
    private ChairController _chairController;
    private IChairView _actorView;
    private PlayerPhysics _actorPhysics;

    public bool IsTransitioning { get; private set; }
    public bool IsSitting { get; private set; }

    public Transform LookAtPoint => _chairController != null ? _chairController.LookAtPoint : null;

    public ChairHelper(PlayerInputHandler inputHandler, IChairView actorView, PlayerPhysics actorPhysics)
    {
        _inputHandler = inputHandler;
        _actorView = actorView;
        _actorPhysics = actorPhysics;
    }

    public void TeleportToSitting(ChairController chairController)
    {
        _chairController = chairController;
        IsTransitioning = false;
        IsSitting = true;

        _chairController.Block();
        _actorPhysics.TeleportToPoint(chairController.SittingPoint);
        _inputHandler.SetScope(EInputScope.PlayerSitting);
    }

    public void StartSitting(ChairController chairController)
    {
        _chairController = chairController;
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
        Transform standingPoint = GetBestStandingPoint(_chairController);
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
}



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
    private enum ESitPhase { None, Approaching, Sitting }
    private ESitPhase _sitPhase;

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
        if (IsSitting)
        {
            _chairController.Unblock();
            _actorPhysics.OnArriveEvent -= OnArrive;
            _actorPhysics.SetTargetPoint(null); // Clear
        }

        _chairController = chairController;
        IsTransitioning = false;
        IsSitting = true;

        _actorView.OnSitting();
        _chairController.Block();
        _actorPhysics.TeleportToPoint(chairController.SittingPoint);
        _inputHandler.SetScope(EInputScope.PlayerSitting);
    }

    public void StartSitting(ChairController chairController)
    {
        _chairController = chairController;
        IsTransitioning = true;
        IsSitting = true;
        _chairController.Block();

        _actorPhysics.OnArriveEvent -= OnArrive;
        _actorPhysics.OnArriveEvent += OnArrive;
        _inputHandler.SetScope(EInputScope.PlayerSitting);

        if (chairController.ApproachPoints != null && chairController.ApproachPoints.Length > 0)
        {
            Transform approachPoint = GetBestApproachPoint(chairController);
            if (NeedsApproach(chairController, approachPoint))
            {
                _sitPhase = ESitPhase.Approaching;
                _actorPhysics.SetTargetPoint(approachPoint);
            }
            else
            {
                _sitPhase = ESitPhase.Sitting;
                _actorPhysics.SetTargetPoint(chairController.SittingPoint);
            }
        }
    }

    private bool NeedsApproach(ChairController chairController, Transform approachPoint)
    {
        Vector3 toPlayer = (_actorPhysics.Position - chairController.SittingPoint.position).normalized;
        float dot = Vector3.Dot(approachPoint.forward, toPlayer);
        return dot < 0.3f; // player is NOT already on the approach side
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
        if (_sitPhase == ESitPhase.Approaching)
        {
            _sitPhase = ESitPhase.Sitting;
            _actorPhysics.SetTargetPoint(_chairController.SittingPoint);
            return; // don't unsubscribe yet, wait for second arrival
        }

        if (IsSitting)
        {
            _actorView.OnSitting();
        }

        _sitPhase = ESitPhase.None;
        _actorPhysics.OnArriveEvent -= OnArrive;
        IsTransitioning = false;
        _actorPhysics.SetTargetPoint(null);
    }

    private Transform GetBestApproachPoint(ChairController chairController)
    {
        Transform[] points = chairController.ApproachPoints;
        if (points == null || points.Length == 0) return null;

        Transform best = null;
        float bestSqrDist = float.MaxValue;
        Vector3 playerPos = _actorPhysics.Position;

        foreach (var point in points)
        {
            float sqrDist = (point.position - playerPos).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = point;
            }
        }
        return best;
    }
}


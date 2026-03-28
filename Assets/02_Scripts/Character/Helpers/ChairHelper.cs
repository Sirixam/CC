
using System;
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
    public event Action OnSittingComplete;


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
        OnSittingComplete?.Invoke(); //callback to handle showing the answersheet upon sitting
    }

    public void TeleportToStanding(Transform standingPoint)
    {
        if (IsSitting)
        {
            _chairController?.Unblock();
            _actorPhysics.OnArriveEvent -= OnArrive;
            _actorPhysics.SetTargetPoint(null);
        }

        _chairController = null;
        IsTransitioning = false;
        IsSitting = false;

        _actorView.OnStanding();
        _actorPhysics.TeleportToPoint(standingPoint);
        _inputHandler.SetScope(EInputScope.PlayerStanding);
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

    public void StartStanding(Vector2 inputDirection)
    {

        Transform standingPoint = GetBestStandingPoint(_chairController, inputDirection);
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
    private Transform GetBestStandingPoint(ChairController chairController, Vector2 inputDirection)
    {
        Transform[] points = chairController.StandingPoints;
        if (points.Length == 0) return null;

        // No input — fall back to random
        if (inputDirection.sqrMagnitude < 0.1f)
            return points[UnityEngine.Random.Range(0, points.Length)];

        // Convert 2D input to world direction relative to chair
        Vector3 worldDir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

        Transform best = points[0];
        float bestDot = -Mathf.Infinity;

        foreach (var point in points)
        {
            Vector3 toPoint = (point.position - chairController.SittingPoint.position).normalized;
            float dot = Vector3.Dot(worldDir, toPoint);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = point;
            }
        }
        return best;
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
            OnSittingComplete?.Invoke();
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


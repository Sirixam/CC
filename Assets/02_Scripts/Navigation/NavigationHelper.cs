using System;
using UnityEngine;
using UnityEngine.AI;

public class NavigationHelper
{
    private enum EState
    {
        Idle,
        Moving,
        WaitingDelay,
    }

    [Serializable]
    public class Data
    {
        public bool AllowRepeatRoutes;
    }

    private Data _data;
    private NavigationManager _navigationManager;
    private NavMeshAgent _navMeshAgent;

    private IActor _actor;
    private int _lastRouteIndex = -1;
    private NavigationManager.WaypointData[] _currentRoute;
    private int _currentWaypointIndex;

    private EState _state;
    private float _remainingWaitTime;

    public NavigationHelper(IActor actor, Data data, NavMeshAgent navMeshAgent, NavigationManager navigationManager)
    {
        _actor = actor;
        _data = data;
        _navMeshAgent = navMeshAgent;
        _navigationManager = navigationManager;
    }

    public void Start()
    {
        GoToNewDestination();
    }

    public void Update()
    {
        if (_state == EState.Moving)
        {
            CheckArrival();
        }
        else if (_state == EState.WaitingDelay)
        {
            _remainingWaitTime -= Time.deltaTime;
            if (_remainingWaitTime <= 0)
            {
                NextWaypoint();
            }
        }
    }

    [Button("Go To Destination")]
    public void GoToNewDestination()
    {
        if (_data.AllowRepeatRoutes)
        {
            _currentRoute = _navigationManager.GetRandomRoute();
        }
        else
        {
            _currentRoute = _navigationManager.GetRandomRouteNoRepeat(ref _lastRouteIndex);
        }
        _currentWaypointIndex = 0;

        MoveToCurrentWaypoint();
    }

    private void MoveToCurrentWaypoint()
    {
        if (_currentRoute == null || _currentWaypointIndex >= _currentRoute.Length)
            return;

        _state = EState.Moving;
        _navMeshAgent.SetDestination(_currentRoute[_currentWaypointIndex].Point.position);
    }

    private void CheckArrival()
    {
        if (_navMeshAgent.pathPending)
            return;

        if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
        {
            if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude == 0f)
            {
                OnArriveAtWaypoint();
            }
        }
    }

    private void OnArriveAtWaypoint()
    {
        NavigationManager.WaypointData waypoint = _currentRoute[_currentWaypointIndex];
        if (waypoint.ArriveEvent != null)
        {
            waypoint.ArriveEvent.Execute(_actor);
        }

        float nextDelay = waypoint.NextDelay;
        if (nextDelay > 0)
        {
            _state = EState.WaitingDelay;
            _remainingWaitTime = nextDelay;
            return;
        }

        NextWaypoint();
    }

    private void NextWaypoint()
    {
        _currentWaypointIndex++;
        if (_currentWaypointIndex < _currentRoute.Length)
        {
            MoveToCurrentWaypoint();
        }
        else
        {
            _state = EState.Idle;
            GoToNewDestination();
        }
    }
}

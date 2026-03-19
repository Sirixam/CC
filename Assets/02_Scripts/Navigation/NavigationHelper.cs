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

    public event Action OnArriveAtDestination;

    public NavigationHelper(IActor actor, Data data, NavMeshAgent navMeshAgent, NavigationManager navigationManager)
    {
        _actor = actor;
        _data = data;
        _navMeshAgent = navMeshAgent;
        _navigationManager = navigationManager;
    }

    public void Start()
    {
        GoToRandomDestination();
    }

    public void Start(string routeName)
    {
        GoToDestination(routeName);
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
                AdvanceOrFinish();
            }
        }
    }

    [Button("Go To Random Destination")]
    public void GoToRandomDestination()
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

    public void GoToDestination(string routeName)
    {
        _currentRoute = _navigationManager.GetRoute(routeName);
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

        AdvanceOrFinish();

    }

    private void AdvanceOrFinish()
    {
        if (HasNextWaypoint())
        {
            _currentWaypointIndex++;
            MoveToCurrentWaypoint();
        }
        else
        {
            _state = EState.Idle;
            if (OnArriveAtDestination != null)
            {
                OnArriveAtDestination.Invoke();
            }
            else
            {
                GoToRandomDestination();
            }
        }
    }

    private bool HasNextWaypoint()
    {
        return _currentWaypointIndex + 1 < _currentRoute.Length;
    }
}

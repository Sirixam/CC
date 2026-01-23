using UnityEngine;
using UnityEngine.AI;

// TODO: Rename and refactor into NavigationHelper.
public class NavigationController : MonoBehaviour
{
    private enum EState
    {
        Idle,
        Moving,
        Wait,
    }

    [SerializeField] private NavigationManager _navigationManager;
    [SerializeField] private NavMeshAgent _navMeshAgent;

    [Header("Configurations")]
    [SerializeField] private bool _allowRepeatRoutes;

    private int _lastRouteIndex = -1;
    private NavigationManager.WaypointData[] _currentRoute;
    private int _currentWaypointIndex;

    private EState _state;
    private float _remainingWaitTime;

    private void Start()
    {
        GoToNewDestination();
    }

    private void Update()
    {
        if (_state == EState.Moving)
        {
            CheckArrival();
        }
        else if (_state == EState.Wait)
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
        _currentRoute = _navigationManager.GetRandomRouteNoRepeat(ref _lastRouteIndex);
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
        float waitTime = _currentRoute[_currentWaypointIndex].WaitTime;
        if (waitTime > 0)
        {
            _state = EState.Wait;
            _remainingWaitTime = waitTime;
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
            OnArriveAtDestination();
        }
    }

    private void OnArriveAtDestination()
    {
        _state = EState.Idle;
        GoToNewDestination(); // TODO: Throw event instead and handle logic inside real teacher controller.
    }
}

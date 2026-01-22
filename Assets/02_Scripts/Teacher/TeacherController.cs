using UnityEngine;
using UnityEngine.AI;

// TODO: Rename and refactor into NavigationHelper.
public class TeacherController : MonoBehaviour
{
    [SerializeField] private NavigationManager _navigationManager;
    [SerializeField] private NavMeshAgent _navMeshAgent;

    [Header("Configurations")]
    [SerializeField] private bool _allowRepeatRoutes;

    private int _lastRouteIndex = -1;
    private Transform[] _currentRoute;
    private int _currentWaypointIndex;

    private bool _isMoving;

    private void Start()
    {
        GoToNewDestination();
    }

    private void Update()
    {
        CheckArrival();
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

        _isMoving = true;
        _navMeshAgent.SetDestination(_currentRoute[_currentWaypointIndex].position);
    }

    private void CheckArrival()
    {
        if (!_isMoving)
            return;

        if (_navMeshAgent.pathPending)
            return;

        if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
        {
            if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude == 0f)
            {
                _isMoving = false;
                OnArriveAtWaypoint();
            }
        }
    }

    private void OnArriveAtWaypoint()
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
        GoToNewDestination(); // TODO: Throw event instead and handle logic inside real teacher controller.
    }
}

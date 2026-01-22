using UnityEngine;
using UnityEngine.AI;

public class TeacherController : MonoBehaviour
{
    [SerializeField] private TeacherManager _routesManager;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [Header("Configurations")]
    [SerializeField] private bool _allowRepeatRoutes;

    private int _lastRouteIndex = -1;

    private void Start()
    {
        GoToNewDestination();
    }

    [Button("Go To Destination")]
    public void GoToNewDestination()
    {
        Transform[] route = _routesManager.GetRandomRouteNoRepeat(ref _lastRouteIndex);
        _navMeshAgent.SetDestination(route[0].position); // TODO: Follow route
    }
}

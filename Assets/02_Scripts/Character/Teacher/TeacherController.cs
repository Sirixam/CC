using UnityEngine;
using UnityEngine.AI;

public class TeacherController : MonoBehaviour, IActor, ILookAroundActor
{
    [SerializeField] private NavigationManager _navigationManager;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private NavigationHelper.Data _navigationData;

    public Transform LookPivot;

    string IActor.ID => IActor.GetStudentNpcID(0); // TODO: Support multiple teachers
    Transform ILookAroundActor.Pivot => transform;

    private NavigationHelper _navigationHelper;

    private void Awake()
    {
        _navigationHelper = new NavigationHelper(this, _navigationData, _navMeshAgent, _navigationManager);
    }

    private void Start()
    {
        _navigationHelper.Start();
    }

    private void Update()
    {
        _navigationHelper.Update();
    }
}

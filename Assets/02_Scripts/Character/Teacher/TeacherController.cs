using UnityEngine;
using UnityEngine.AI;

public class TeacherController : MonoBehaviour, IActor, ILookAroundActor, ISitActor
{
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private NavigationManager _navigationManager;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private NavigationHelper.Data _navigationData;
    [SerializeField] private TeacherAudioHelper.Data _audioData;
    [Header("Configurations")]
    [SerializeField] private string _seatRouteName = "TeacherSeat";

    string IActor.ID => IActor.GetStudentNpcID(0); // TODO: Support multiple teachers
    Transform ILookAroundActor.Pivot => transform;

    private NavigationHelper _navigationHelper;
    private TeacherAudioHelper _audioHelper;

    private void Awake()
    {
        _audioHelper = new TeacherAudioHelper(_audioData);
        _navigationHelper = new NavigationHelper(this, _navigationData, _navMeshAgent, _navigationManager);
        //_fieldOfViewController.HideInstant();
    }

    private void Start()
    {
        _navigationHelper.Start(_seatRouteName);
    }

    private void Update()
    {
        _navigationHelper.Update();
    }

    void ISitActor.ExecuteSit()
    {
        _fieldOfViewController.Hide();
    }
}

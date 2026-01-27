using System;
using UnityEngine;
using UnityEngine.AI;

public class TeacherController : MonoBehaviour, IActor, ILookAroundActor, ISitActor
{
    public enum EState
    {
        GoToSeat,
        Sit,
        Patrol,
    }

    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private NavigationHelper.Data _navigationData;
    [SerializeField] private TeacherAudioHelper.Data _audioData;
    [SerializeField] private GlobalDefinition _globalDefinition;
    [Header("Configurations")]
    [SerializeField] private Vector2 _timeToStandRange = new Vector2(5, 5);
    [SerializeField] private Vector2 _timeToSitRange = new Vector2(10, 10);
    [SerializeField] private string _seatRouteName = "TeacherSeat";

    string IActor.ID => IActor.GetStudentNpcID(0); // TODO: Support multiple teachers
    Transform ILookAroundActor.Pivot => transform;

    private EState _state;
    private float _remainingTime;
    private bool _goToSeatOnArrive;

    private NavigationHelper _navigationHelper;
    private TeacherAudioHelper _audioHelper;

    public Action<PlayerController> OnPlayerDetected;
    public Action<IItemController> OnItemDetected;

    private void Awake()
    {
        _audioHelper = new TeacherAudioHelper(_audioData);
        _fieldOfViewController.HideInstant();
    }

    public void Inject(NavigationManager navigationManager)
    {
        _navigationHelper = new NavigationHelper(this, _navigationData, _navMeshAgent, navigationManager);
        _navigationHelper.OnArriveAtDestination += OnArriveAtDestination;
    }

    private void Start()
    {
        GoToSeat();
    }

    private void Update()
    {
        _navigationHelper.Update();

        _remainingTime -= Time.deltaTime;
        if (_remainingTime <= 0)
        {
            if (_state == EState.Sit)
            {
                Stand();

                _state = EState.Patrol;
                _navigationHelper.GoToRandomDestination();
            }
            else
            {
                _goToSeatOnArrive = true;
            }
        }
    }

    private void OnArriveAtDestination()
    {
        if (_state == EState.Patrol)
        {
            if (_goToSeatOnArrive)
            {
                _goToSeatOnArrive = false;
                GoToSeat();
            }
            else
            {
                _navigationHelper.GoToRandomDestination();
            }
        }
    }

    private void GoToSeat()
    {
        _state = EState.GoToSeat;
        _navigationHelper.Start(_seatRouteName);
    }

    void ISitActor.ExecuteSit()
    {
        _state = EState.Sit;
        _fieldOfViewController.Hide();
        _remainingTime = UnityEngine.Random.Range(_timeToStandRange.x, _timeToStandRange.y);
    }

    private void Stand()
    {
        _fieldOfViewController.Show();
        _remainingTime = UnityEngine.Random.Range(_timeToSitRange.x, _timeToSitRange.y);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_globalDefinition.PlayerTag))
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            OnPlayerDetected?.Invoke(playerController);
            _audioHelper.OnGettingCaught();

        }
        else if (other.gameObject.layer == _globalDefinition.ItemLayer || other.gameObject.layer == _globalDefinition.FlyingLayer)
        {
            IItemController itemController = other.GetComponentInParent<IItemController>();
            if (itemController == null)
            {
                Debug.LogError("Other collider has item tag, but has not implemented item controller. Name: " + other.name);
            }
            else
            {
                OnItemDetected?.Invoke(itemController);
            }
        }
    }
}

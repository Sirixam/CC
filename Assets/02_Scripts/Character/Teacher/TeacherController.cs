using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;


public class TeacherController : MonoBehaviour, IActor, ILookAroundActor, ISitActor
{
    public enum EState
    {
        GoToSeat,
        Sit,
        Patrol,
    }

    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TriggerListener _detectionTriggerListener;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private NavigationHelper.Data _navigationData;
    [SerializeField] private TeacherAudioHelper.Data _audioData;
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private NavMeshObstacle _walkBackObstacle;
    [SerializeField] private Collider _collider;
    [SerializeField] private TeacherView _teacherView;
    public Collider Collider => _collider;

    [Header("Configurations")]
    [SerializeField] private Vector2 _timeToStandRange = new Vector2(5, 5);
    [SerializeField] private Vector2 _timeToSitRange = new Vector2(10, 10);
    [SerializeField] private string _seatRouteName = "TeacherSeat";

    string IActor.ID => IActor.GetStudentNpcID(0); // TODO: Support multiple teachers
    Transform ILookAroundActor.Pivot => transform;

    private EState _state;
    private float _remainingTime;
    private bool _goToSeatOnArrive;
    private bool _isActive;

    private NavigationHelper _navigationHelper;
    private TeacherAudioHelper _audioHelper;

    public Action<PlayerController> OnPlayerDetected;
    public Action<IItemController> OnItemDetected;
    
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private float _detectionCooldown;
    public void PlayAngryVFX() => _teacherView.PlayAngryVFX();
    public void StopAngryVFX() => _teacherView.StopAngryVFX();


    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        
        _audioHelper = new TeacherAudioHelper(_audioData);
        _fieldOfViewController.HideInstant();
        _detectionTriggerListener.OnEnter += OnDetectionTriggerEnter;
    }

    private void OnDestroy()
    {
        _detectionTriggerListener.OnEnter -= OnDetectionTriggerEnter;
    }

    public void Inject(NavigationManager navigationManager)
    {
        _navigationHelper = new NavigationHelper(this, _navigationData, _navMeshAgent, navigationManager);
        _navigationHelper.OnArriveAtDestination += OnArriveAtDestination;
    }

    private void Start()
    {
        _state = EState.Sit;
        _isActive = false;
    }

    private void Update()
    {

        if (_detectionCooldown > 0)
            _detectionCooldown -= Time.deltaTime;

        if (!_isActive) return;

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
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
    }

    private void Stand()
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.updateRotation = true;
            _navMeshAgent.isStopped = false;
        }

        _fieldOfViewController.Show();
        _remainingTime = UnityEngine.Random.Range(_timeToSitRange.x, _timeToSitRange.y);
    }

    private void OnDetectionTriggerEnter(Collider other)
    {
        if (_detectionCooldown > 0) return;

        if (other.CompareTag(_globalDefinition.PlayerTag))
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();

            if (playerController != null && playerController.IsSitting)
                return;

            _detectionCooldown = 1f; // ignore further detections for 1 second
            OnPlayerDetected?.Invoke(playerController);
            _teacherView.PlayAngryVFX();
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

    public void ResetTeacher()
    {
        StopAllCoroutines();
        _teacherView.StopAngryVFX();


        _navigationHelper.Reset();
        _detectionCooldown = 0f;

        // Stop movement immediately
        if (_navMeshAgent != null)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
            _navMeshAgent.velocity = Vector3.zero;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.Warp(_initialPosition);
        }

        // Reset transform
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // Reset state machine
        _state = EState.Sit;
        _goToSeatOnArrive = false;
        
        // Reset teacher
        _isActive = false;

        // Reset vision
        _fieldOfViewController.HideInstant();

    }

    public void StartPatrolling()
    {
        _isActive = true;
        _state = EState.Sit;
        _remainingTime = 1f;
    }

    public void PauseAndLookAt(Transform target, float duration, System.Action onComplete)
    {
        StartCoroutine(PauseAndLookAtRoutine(target, duration, onComplete));
    }

    private IEnumerator PauseAndLookAtRoutine(Transform target, float duration, System.Action onComplete)
    {
        _navMeshAgent.isStopped = true;
        _navMeshAgent.updateRotation = false;
        _navMeshAgent.velocity = Vector3.zero;
        _isActive = false;

        // Cache original FOV values
        float originalDistance = _fieldOfViewController.GetMaxDistance();
        float originalWidth = _fieldOfViewController.GetWidthScale();

        // Rotate toward target
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotateTime = 0.3f;
        float t = 0f;
        Quaternion startRotation = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime / rotateTime;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, target.position) + 1f; // overshoot slightly

        // Narrow FOV into a line and extend to reach the desk
        float narrowTime = 0.3f;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / narrowTime;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float newWidth = Mathf.Lerp(originalWidth, 0f, eased);
            float newDistance = Mathf.Lerp(originalDistance, distanceToTarget, eased);
            _fieldOfViewController.SetFOVParams(newDistance, newWidth);
            yield return null;
        }

        // Hold the look
        yield return new WaitForSeconds(duration);

        // Restore FOV back to normal
        float restoreTime = 0.3f;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / restoreTime;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float newWidth = Mathf.Lerp(0f, originalWidth, eased);
            float newDistance = Mathf.Lerp(distanceToTarget, originalDistance, eased);
            _fieldOfViewController.SetFOVParams(newDistance, newWidth);
            yield return null;
        }

        // Resume patrolling
        _navMeshAgent.updateRotation = true;
        _navMeshAgent.isStopped = false;
        _isActive = true;

        onComplete?.Invoke();
    }
    public void PauseAndFollowTarget(Transform target, System.Action onComplete)
    {
        StopAllCoroutines();
        _navigationHelper.Reset();
        StartCoroutine(FollowTargetRoutine(target, onComplete));
    }

    private IEnumerator FollowTargetRoutine(Transform target, System.Action onComplete)
    {
        Debug.Log("FollowTargetRoutine: STARTED, disabling agent");

        _navMeshAgent.isStopped = true;
        _navMeshAgent.ResetPath();
        _navMeshAgent.updateRotation = false;
        _navMeshAgent.velocity = Vector3.zero;
        _navMeshAgent.enabled = false;
        _isActive = false;

        if (_walkBackObstacle != null)
            _walkBackObstacle.enabled = true;

        float originalDistance = _fieldOfViewController.GetMaxDistance();
        float originalWidth = _fieldOfViewController.GetWidthScale();

        PlayerController player = target.GetComponent<PlayerController>();

        Debug.Log($"FollowTargetRoutine: entering while loop, IsSitting: {player.IsSitting}, IsCaught: {player.IsCaught}");

        while (target != null && target.gameObject.activeInHierarchy)
        {
            if (player != null && player.IsSitting && !player.IsCaught)
            {
                Debug.Log("FollowTargetRoutine: player sat down, breaking");
                break;
            }

            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }

            yield return null;
        }

        Debug.Log("FollowTargetRoutine: while loop exited, restoring");

        // Small pause before resuming
        yield return new WaitForSeconds(0.5f);

        // Restore FOV
        float restoreTime = 0.3f;
        float currentDistance = _fieldOfViewController.GetMaxDistance();
        float currentWidth = _fieldOfViewController.GetWidthScale();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / restoreTime;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            _fieldOfViewController.SetFOVParams(
                Mathf.Lerp(currentDistance, originalDistance, eased),
                Mathf.Lerp(currentWidth, originalWidth, eased)
            );
            yield return null;
        }

        if (_walkBackObstacle != null)
            _walkBackObstacle.enabled = false;

        // Wait one frame for obstacle to uncarve
        yield return null;

        Vector3 currentPos = transform.position;
        _navMeshAgent.enabled = true;

        // Sample nearest valid navmesh point
        if (UnityEngine.AI.NavMesh.SamplePosition(currentPos, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            _navMeshAgent.Warp(hit.position);
        }
        else
        {
            _navMeshAgent.Warp(currentPos);
        }

        _navMeshAgent.updateRotation = true;
        _navMeshAgent.isStopped = false;
        _isActive = true;

        _state = EState.Patrol;
        _remainingTime = UnityEngine.Random.Range(_timeToSitRange.x, _timeToSitRange.y);
        _navigationHelper.GoToRandomDestination();

        onComplete?.Invoke();
    }
}

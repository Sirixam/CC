using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TriggerListener _fieldOfViewTriggerListener;
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private DistractionUI _distractionUI;
    [SerializeField] private LightbulbUI _lightbulbUI;
    [SerializeField] private StudentView _studentView;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private DistractionHelper.Data _distractionData;
    [SerializeField] private StudentAudioHelper.Data _audioData;
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private TestDefinition _testDefinitionOverride;
    [SerializeField] private Sprite _icon;
    [SerializeField] private Sprite _archetypeIcon;
    [SerializeField] private bool _canDetectItems;
    [SerializeField] private bool _canDetectFlyingItems;

    // Runtime    
    private TestDefinition _testDefinition;
    public bool IsDistracted => _distractionHelper.IsDistracted;
    public bool IsDetecting { get; private set; }
    private ChairController _chairController;
    public AnswerController AnswerController { get; private set; }
    public Sprite CharacterIcon => _icon;
    public Sprite ArchetypeIcon => _archetypeIcon;

    // Helpers
    private LookHelper _lookHelper;
    private DistractionHelper _distractionHelper;
    private StudentAudioHelper _audioHelper;

    // Events
    public Action<PlayerController> OnPlayerDetected;
    public Action<IItemController> OnItemDetected;
    public event Action OnAnsweringEnded;

    private void Awake()
    {
        AnswerController = transform.parent.GetComponentInChildren<AnswerController>();
        _chairController = transform.parent.GetComponentInChildren<ChairController>();

        // Helpers
        _lookHelper = new LookHelper(_lookData);
        _audioHelper = new StudentAudioHelper(_audioData);
        _distractionHelper = new DistractionHelper(_distractionData, _distractionUI, _fieldOfViewController, _lookHelper, AnswerController, _audioHelper);

        // Initialize
        _stateText.text = "Idle";
        InjectTestDefinition(_testDefinitionOverride ?? _testDefinition); // Reinject in case it was injected before awake
        _lookHelper.Initialize(transform.forward);
        AnswerController.BlockCheat();
        _fieldOfViewController.HideInstant();
        _distractionUI.Hide();
        _lightbulbUI.Hide();
        _chairController.OnCollisionEnterEvent += OnCollisionEnter;
        _fieldOfViewTriggerListener.OnEnter += OnDetectionTriggerEnter;
    }

    private void OnDestroy()
    {
        _chairController.OnCollisionEnterEvent = OnCollisionEnter;
        _fieldOfViewTriggerListener.OnEnter -= OnDetectionTriggerEnter;
    }

    private void Update()
    {
        _lookHelper.UpdateRotation(transform);
    }

    public void InjectTestDefinition(TestDefinition testDefinition)
    {
        _testDefinition = testDefinition;
        _distractionHelper?.InjectTestDefinition(testDefinition);
    }

    public void SetDurations(float thinkingDuration, float answeringDuration, float validatingDuration)
    {
        //float answeringDuration = AnswerController.GetAnsweringDuration();
        AnswerController.SetDurations(thinkingDuration, answeringDuration, validatingDuration);
    }

    public void SetCorrectness(string answerID, float value)
    {
        AnswerController.SetCorrectness(answerID, value);
    }

    public void StartThinking()
    {
        _stateText.text = "Thinking";
        AnswerController.StartThinking();
        _lightbulbUI.Show();
        _lightbulbUI.SetState(isOn: false);
        _lightbulbUI.StartFloat();
    }

    public void StartAnswering()
    {
        _stateText.text = ""; // TODO: REMOVE
        AnswerController.StartAnswering(progress: 0);
        _lightbulbUI.SetState(isOn: true);
        _lightbulbUI.PlayShine();
        _lightbulbUI.HideDelayed();
        _studentView.StartAnswering();
    }

    public void StartValidating()
    {
        _stateText.text = "Validating";
        AnswerController.StartValidating();
        _studentView.StartValidating();
    }

    public async UniTask UpdateRemainingTimeWhileNotDistracted(CancellationToken cancellationToken)
    {
        bool finished = false;
        while (!finished && !cancellationToken.IsCancellationRequested)
        {
            if (!IsDistracted)
            {
                AnswerController.UpdateRemainingTime(Time.deltaTime, out finished);
            }
            await UniTask.Yield(cancellationToken);
        }
    }

    public async UniTask UpdateAnsweringTaskWhileNotDistracted(CancellationToken cancellationToken)
    {
        bool finished = false;
        while (!finished && !cancellationToken.IsCancellationRequested)
        {
            if (!IsDistracted)
            {
                AnswerController.UpdateAnswering(Time.deltaTime, out finished);
            }
            await UniTask.Yield(cancellationToken);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            _lightbulbUI.Hide();
            OnAnsweringEnded?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(_globalDefinition.DistractionTag))
        {
            PaperBallController paperBall = collision.collider.GetComponentInParent<PaperBallController>();
            if (paperBall != null && paperBall.HasHitGround)
                return;

            Vector3 hitDirection = Vector3.zero;
            foreach (var contact in collision.contacts)
            {
                hitDirection = (contact.point - transform.position).normalized;
            }

            _distractionHelper.OnDistracted(hitDirection).Forget();
        }
    }

    private void OnDetectionTriggerEnter(Collider other)
    {
        if (other.CompareTag(_globalDefinition.PlayerTag))
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            OnPlayerDetected?.Invoke(playerController);
        }
        else if ((_canDetectItems && other.gameObject.layer == _globalDefinition.ItemLayer) || (_canDetectFlyingItems && other.gameObject.layer == _globalDefinition.FlyingLayer))
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

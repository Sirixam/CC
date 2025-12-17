using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private StudentAudio _audio;
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private DistractionUI _distractionUI;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private DistractionHelper.Data _distractionData;
    [SerializeField, Tag] private string _playerTag = "Player";

    // Runtime    
    public bool IsDistracted => _distractionHelper.IsDistracted;
    public bool IsDetecting { get; private set; }
    private ChairController _chairController;
    public AnswerController AnswerController { get; private set; }

    // Helpers
    private LookHelper _lookHelper;
    private DistractionHelper _distractionHelper;

    public Action<PlayerController> OnPlayerDetected;

    private void Awake()
    {
        AnswerController = transform.parent.GetComponentInChildren<AnswerController>();
        _chairController = transform.parent.GetComponentInChildren<ChairController>();

        // Helpers
        _lookHelper = new LookHelper(_lookData);
        _distractionHelper = new DistractionHelper(_distractionData, _distractionUI, _fieldOfViewController, _lookHelper, AnswerController, _audio);

        // Initialize
        _stateText.text = "Idle";
        _lookHelper.Initialize(transform.forward);
        AnswerController.BlockCheat();
        _fieldOfViewController.Hide();
        _distractionUI.Hide();
        _chairController.OnCollisionEnterEvent += OnCollisionEnter;
    }

    private void OnDestroy()
    {
        _chairController.OnCollisionEnterEvent = OnCollisionEnter;
    }

    private void Update()
    {
        _lookHelper.UpdateRotation(transform);
        AnswerController.UpdateRemainingTime(Time.deltaTime);
    }

    public void SetRemainingTimes(float thinkingTime, float validatingTime)
    {
        float answeringTime = AnswerController.GetAnsweringDuration();
        AnswerController.SetRemainingTimes(thinkingTime, answeringTime, validatingTime);
    }

    public void StartThinking()
    {
        _stateText.text = "Thinking";
        AnswerController.StartThinking();
    }

    public void StartAnswering()
    {
        _stateText.text = "Answering";
        AnswerController.StartAnswering(progress: 0);
    }

    public void StartValidating()
    {
        _stateText.text = "Validating";
        AnswerController.StartValidating();
    }

    public async UniTask WaitWhileNotDistracted(float timer, CancellationToken cancellationToken)
    {
        while (timer > 0 && !cancellationToken.IsCancellationRequested)
        {
            if (!IsDistracted)
            {
                timer -= Time.deltaTime;
            }
            await UniTask.Yield(cancellationToken);
        }
    }

    public async UniTask UpdateAnsweringTask(CancellationToken cancellationToken)
    {
        bool finishedAnswering = false;
        while (!finishedAnswering && !IsDistracted)
        {
            AnswerController.UpdateAnswering(out finishedAnswering);
            await UniTask.Yield(cancellationToken);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(_distractionData.DistractionTag))
        {
            Vector3 hitDirection = Vector3.zero;
            foreach (var contact in collision.contacts)
            {
                hitDirection = (contact.point - transform.position).normalized;
            }

            _distractionHelper.OnDistracted(hitDirection).Forget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_playerTag))
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            OnPlayerDetected?.Invoke(playerController);
        }
    }
}

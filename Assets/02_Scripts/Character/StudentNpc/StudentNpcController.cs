using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TMP_Text _stateText;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private float _distractionDuration = 5f;
    [SerializeField] private float _distractionRotationDelay = 1f;
    [SerializeField][Tag] private string _playerTag = "Player";
    [SerializeField][Tag] private string _distractionTag = "Distraction";

    // Runtime    
    public bool IsDistracted { get; private set; }
    public bool IsDetecting { get; private set; }
    private ChairController _chairController;
    public AnswerController AnswerController { get; private set; }

    // Helpers
    private LookHelper _lookHelper;

    public Action<PlayerController> OnPlayerDetected;

    private void Awake()
    {
        AnswerController = transform.parent.GetComponentInChildren<AnswerController>();
        _chairController = transform.parent.GetComponentInChildren<ChairController>();

        // Helpers
        _lookHelper = new LookHelper(_lookData);

        // Initialize
        _stateText.text = "Idle";
        _lookHelper.Initialize(transform.forward);
        AnswerController.BlockCheat();
        _fieldOfViewController.Hide();
        _chairController.OnCollisionEnterEvent += OnCollisionEnter;
    }

    private void OnDestroy()
    {
        _chairController.OnCollisionEnterEvent = OnCollisionEnter;

    }

    private void Update()
    {
        _lookHelper.UpdateRotation(transform);
    }

    public void StartThinking()
    {
        _stateText.text = "Thinking";
    }

    public void StartAnswering()
    {
        _stateText.text = "Answering";
        AnswerController.StartAnswering(progress: 0);
    }

    public void StartValidating()
    {
        _stateText.text = "Validating";
    }

    private async UniTask OnDistracted(Vector3 hitDirection)
    {
        if (IsDistracted) return;

        string initialState = _stateText.text;

        IsDistracted = true;
        _stateText.text = "Distracted";
        AnswerController.UnblockCheat();

        await UniTask.WaitForSeconds(_distractionRotationDelay);

        Vector2 lookDirection = new Vector2(hitDirection.x, hitDirection.z).normalized;
        _lookHelper.SetLookInput(lookDirection);
        _fieldOfViewController.Show();

        await UniTask.WaitForSeconds(_distractionDuration - _distractionRotationDelay);

        IsDistracted = false;
        AnswerController.BlockCheat();
        _fieldOfViewController.Hide();
        _lookHelper.RestoreInitialLookDirection();
        _stateText.text = initialState;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(_distractionTag))
        {
            Vector3 hitDirection = Vector3.zero;
            foreach (var contact in collision.contacts)
            {
                hitDirection = (contact.point - transform.position).normalized;
            }

            OnDistracted(hitDirection).Forget();
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

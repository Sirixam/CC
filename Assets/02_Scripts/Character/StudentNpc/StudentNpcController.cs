using TMPro;
using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TMP_Text _stateText;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private float _distractedDuration;
    [SerializeField][Tag] private string _distractionTag = "Distraction";

    // Runtime
    private float _distractedTimer;

    public bool IsDistracted { get; private set; }
    public AnswerController AnswerController { get; private set; }

    // Helpers
    private LookHelper _lookHelper;

    private void Awake()
    {
        AnswerController = transform.parent.GetComponentInChildren<AnswerController>();

        // Helpers
        _lookHelper = new LookHelper(_lookData);

        // Initialize
        _stateText.text = "Idle";
        _lookHelper.Initialize(transform.forward);
        AnswerController.BlockCheat();
        _fieldOfViewController.Hide();
    }

    private void Update()
    {
        if (IsDistracted)
        {
            _distractedTimer -= Time.deltaTime;
            if (_distractedTimer <= 0)
            {
                OnDistractedEnd();
            }
        }
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

    private void OnDistractedStart()
    {
        if (IsDistracted) return;

        IsDistracted = true;
        _distractedTimer = _distractedDuration;
        _stateText.text = "Distracted";
        AnswerController.UnblockCheat();
    }

    private void OnDistractedEnd()
    {
        IsDistracted = false;
        AnswerController.BlockCheat();
        if (AnswerController.IsAnswering)
        {
            _stateText.text = "Answering";
        }
        else if (AnswerController.IsValidatingAnswer)
        {
            _stateText.text = "Validating";
        }
        else
        {
            _stateText.text = "Thinking";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_distractionTag))
        {
            OnDistractedStart();
        }
    }
}

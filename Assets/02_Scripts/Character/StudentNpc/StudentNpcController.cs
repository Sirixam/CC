using TMPro;
using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TMP_Text _stateText;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;

    // Runtime
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
        _fieldOfViewController.Hide();
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
}

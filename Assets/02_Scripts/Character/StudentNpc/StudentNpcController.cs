using TMPro;
using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private AnswerController _answerController;
    [SerializeField] private FieldOfViewController _fieldOfViewController;
    [SerializeField] private TMP_Text _stateText;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;

    // Runtime
    public AnswerController AnswerController => _answerController;

    // Helpers
    private LookHelper _lookHelper;

    private void Awake()
    {
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
    }

    public void StartValidating()
    {
        _stateText.text = "Validating";
    }
}

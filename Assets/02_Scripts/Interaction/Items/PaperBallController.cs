using UnityEngine;

public class PaperBallController : MonoBehaviour, IPickUpInteractionOwner, IItemController
{
    public enum EState
    {
        Undefined,
        Idle,
        MidAir,
        PickedUp,
    }

    [Tooltip("Use 0 if there's no answer in this paper ball")]
    [SerializeField] private AnswerDefinition _defaultAnswerDefinition;
    [SerializeField] private ItemAudioHelper.Data _audioData;
    [SerializeField] private float _timeToDestroyOnIdle = 5f;
    [SerializeField] private bool _destroyOnIdle = true;

    private ItemAudioHelper _audioHelper;
    private string _answerID;
    private float _remainingTimeToDestroyOnIdle;
    private EState _state;
    private string _lastOwnerID;

    public bool HasAnswer => !string.IsNullOrWhiteSpace(_answerID) || _defaultAnswerDefinition != null;
    public string AnswerID => !string.IsNullOrWhiteSpace(_answerID) ? _answerID : _defaultAnswerDefinition != null ? _defaultAnswerDefinition.ID : null;

    public InteractionController InteractionController => GetComponentInChildren<InteractionController>();

    string IItemController.LastOwnerID => _lastOwnerID;

    private void Awake()
    {
        _audioHelper = new ItemAudioHelper(_audioData);
    }

    private void Start()
    {
        if (HasAnswer)
        {
            AnswersManager.GetInstance().OnAllPlayersFinishedAnswer += OnAllPlayersAnsweredFullyEvent;
        }
    }

    private void OnDestroy()
    {
        AnswersManager answersManager = AnswersManager.GetInstance();
        if (answersManager != null)
        {
            answersManager.OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
        }
    }

    private void Update()
    {
        if (_state == EState.Idle && _destroyOnIdle)
        {
            _remainingTimeToDestroyOnIdle -= Time.deltaTime;
            if (_remainingTimeToDestroyOnIdle <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public void SetAnswer(string answerID)
    {
        bool hadAnswer = HasAnswer;
        _answerID = answerID;

        if (hadAnswer != HasAnswer)
        {
            if (hadAnswer)
            {
                AnswersManager.GetInstance().OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
            }
            else
            {
                AnswersManager.GetInstance().OnAllPlayersFinishedAnswer += OnAllPlayersAnsweredFullyEvent;
            }
        }
    }

    private void OnAllPlayersAnsweredFullyEvent(string answerID)
    {
        if (AnswerID != answerID) return;
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment") || collision.gameObject.CompareTag("NPC"))
        {
            _audioHelper.OnCollide(collision);
            if (_state == EState.MidAir)
            {
                SetIdleState();
            }
        }
    }

    private void SetIdleState()
    {
        _state = EState.Idle;
        _remainingTimeToDestroyOnIdle = _timeToDestroyOnIdle;
    }

    // IPickUpInteractionOwner
    void IPickUpInteractionOwner.OnPickedUp(string actorID)
    {
        _lastOwnerID = actorID;
        _state = EState.PickedUp;
    }
    void IPickUpInteractionOwner.OnDropped()
    {
        SetIdleState();
    }
    void IPickUpInteractionOwner.OnThrowed()
    {
        _state = EState.MidAir;
    }
    
    public void OnDetectedByTeacher()
    {
        if (!_destroyOnIdle)
        {
            Destroy(gameObject);
        }
    }
}

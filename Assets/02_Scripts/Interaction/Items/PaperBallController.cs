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
    [SerializeField] private float _defaultCorrectness;
    //[SerializeField] private ItemAudioHelper.Data _audioData;
    [SerializeField] private float _timeToDestroyOnIdle = 5f;
    [SerializeField] private bool _destroyOnIdle = true;
    [SerializeField] private GlobalDefinition _globalDefinition;

    //private ItemAudioHelper _audioHelper;
    private string _answerID;
    private float _correctness;
    private string _contributorActorID;
    private float _remainingTimeToDestroyOnIdle;
    private EState _state;
    private string _lastOwnerID;
    private bool _hasHitGround;
    public bool HasHitGround => _hasHitGround;

    public string ID { get; private set; }
    public bool HasAnswer => !string.IsNullOrWhiteSpace(_answerID) || _defaultAnswerDefinition != null;
    public string AnswerID => !string.IsNullOrWhiteSpace(_answerID) ? _answerID : _defaultAnswerDefinition != null ? _defaultAnswerDefinition.ID : null;
    public float Correctness => !string.IsNullOrWhiteSpace(_answerID) ? _correctness : _defaultCorrectness;
    public string ContributorActorID => !string.IsNullOrWhiteSpace(_answerID) ? _contributorActorID : null;

    public InteractionController InteractionController => GetComponentInChildren<InteractionController>();

    string IItemController.LastOwnerID => _lastOwnerID;
    public EState State => _state;

    private void Awake()
    {
        //_audioHelper = new ItemAudioHelper(_audioData);
        ID = GameContext.ItemsManager.GetNewItemID();
    }

    private void Start()
    {
        if (HasAnswer && GameContext.HasAnswersManager)
        {
            GameContext.AnswersManager.OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
            GameContext.AnswersManager.OnAllPlayersFinishedAnswer += OnAllPlayersAnsweredFullyEvent;
        }
    }

    private void OnDestroy()
    {
        if (GameContext.HasAnswersManager)
        {
            GameContext.AnswersManager.OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
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

    public void SetAnswer(string answerID, float correctness, string contributorActorID)
    {
        bool hadAnswer = HasAnswer;
        _answerID = answerID;
        _correctness = correctness;
        _contributorActorID = contributorActorID;

        if (hadAnswer != HasAnswer && GameContext.HasAnswersManager)
        {
            if (hadAnswer)
            {
                GameContext.AnswersManager.OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
            }
            else
            {
                GameContext.AnswersManager.OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
                GameContext.AnswersManager.OnAllPlayersFinishedAnswer += OnAllPlayersAnsweredFullyEvent;
            }
        }
    }

    private void OnAllPlayersAnsweredFullyEvent(string answerID, float minCorrectness)
    {
        if (AnswerID != answerID) return;
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment")
            || collision.gameObject.layer == LayerMask.NameToLayer("Floor")
            || collision.gameObject.CompareTag("NPC"))
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Floor"))
            {
                _hasHitGround = true;
            }

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
        _hasHitGround = false;
    }

    public void OnDetectedByTeacher()
    {
        bool isHeld =
            InteractionController != null &&
            !InteractionController.enabled;

        Debug.Log(
            $"[PaperBall] OnDetectedByTeacher | Held={isHeld} | State={_state} | Owner={_lastOwnerID}",
            this
        );

        if (isHeld)
            return;

        Destroy(gameObject);
    }

    public bool HasBeenThrown()
    {
        return _state == EState.MidAir || _state == EState.Idle;
    }
}

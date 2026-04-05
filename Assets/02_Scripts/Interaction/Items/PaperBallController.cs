using System.Collections;
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
    [SerializeField] private bool _isLobShot;
    [SerializeField] private bool _isDynamicLobShot;
    [SerializeField] private bool _isAnswer;
    [SerializeField] private float _destroyAfterHitDelay = 0.5f;
    [SerializeField] private bool _isPlane;
    
    private bool _hasBeenThrown;
    private bool _destroyScheduled;
    private bool _hasDropped;


    public bool IsLobShot => _isLobShot;
    public bool IsDynamicLobShot => _isDynamicLobShot;
    public bool IsPlane => _isPlane;


    //private ItemAudioHelper _audioHelper;
    private string _answerID;
    private float _correctness;
    private string _contributorActorID;
    private float _remainingTimeToDestroyOnIdle;
    private EState _state;
    private string _ownerID;
    private string _lastOwnerID;
    private bool _hasHitGround;
    private Collider[] _colliders;
    public bool HasHitGround => _hasHitGround;

    public string ID { get; private set; }
    public bool HasAnswer => !string.IsNullOrWhiteSpace(_answerID) || _defaultAnswerDefinition != null;
    public string AnswerID => !string.IsNullOrWhiteSpace(_answerID) ? _answerID : _defaultAnswerDefinition != null ? _defaultAnswerDefinition.ID : null;
    public float Correctness => !string.IsNullOrWhiteSpace(_answerID) ? _correctness : _defaultCorrectness;
    public string ContributorActorID => !string.IsNullOrWhiteSpace(_answerID) ? _contributorActorID : null;

    public bool IsIdle => _state == EState.Idle;
    public bool IsMidAir => _state == EState.MidAir;
    public bool IsBeingHeld => _state == EState.PickedUp;

    public InteractionController InteractionController => GetComponentInChildren<InteractionController>();

    public string OwnerID => _ownerID;
    public string LastOwnerID => _lastOwnerID;

    private void Awake()
    {
        //_audioHelper = new ItemAudioHelper(_audioData);
        ID = GameContext.ItemsManager.GetNewItemID();
        _colliders = GetComponentsInChildren<Collider>();
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
        // Plane velocity check — drop if too slow
        if (_isPlane && _hasBeenThrown && !_hasDropped)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb.velocity.magnitude < 0.5f)
            {
                _hasDropped = true;
                rb.useGravity = true;
            }
        }

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
        if (_hasBeenThrown && !_destroyScheduled && !_isAnswer)
        {
            _destroyScheduled = true;
            StartCoroutine(DestroyAfterDelay());
        }

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

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (var col in _colliders)
        {
            col.enabled = enabled;
        }
    }

    // IPickUpInteractionOwner
    void IPickUpInteractionOwner.OnPickedUp(string actorID)
    {
        _ownerID = actorID;
        _state = EState.PickedUp;
        _destroyScheduled = false;
        _hasBeenThrown = false;
        StopAllCoroutines(); // Cancel any pending DestroyAfterDelay
        SetCollidersEnabled(false);
    }
    void IPickUpInteractionOwner.OnDropped()
    {
        _lastOwnerID = _ownerID;
        _ownerID = null;
        SetCollidersEnabled(true);
        SetIdleState();
    }
    void IPickUpInteractionOwner.OnThrowed()
    {
        _hasBeenThrown = true;
        _hasDropped = false;
        _lastOwnerID = _ownerID;
        _ownerID = null;
        _state = EState.MidAir;
        _hasHitGround = false;
        SetCollidersEnabled(true);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    public bool HasBeenThrown()
    {
        return _state == EState.MidAir || _state == EState.Idle;
    }
    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyAfterHitDelay);
        Destroy(gameObject);
    }

}

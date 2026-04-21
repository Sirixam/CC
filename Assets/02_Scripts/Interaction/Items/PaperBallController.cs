using System;
using System.Collections;
using PrimeTween;
using UnityEngine;

public class PaperBallController : MonoBehaviour, IPickUpInteractionOwner, IItemController
{
    public enum EState
    {
        Undefined,
        Idle,
        MidAir,
        BeingHeld,
        Confiscated,
    }

    public enum EIdleSource
    {
        FromStart,
        FromMidAir,
        FromDrop
    }

    [Serializable]
    public class DestroyData
    {
        public EIdleSource Type;
        public float Delay;
    }

    [SerializeField] private Transform _rendererContainer;
    [SerializeField] private ParticleSystem _confiscateVFXPrefab;
    [SerializeField] private float _confiscateShrinkDuration = 0.4f;

    [Header("Warning")]
    [SerializeField] private float _warningTime = 2f;
    [SerializeField] private float _warningPulseScale = 1.25f;
    [SerializeField] private float _warningPulseDuration = 0.15f;

    [Tooltip("Use 0 if there's no answer in this paper ball")]
    [SerializeField] private AnswerDefinition _defaultAnswerDefinition;
    [SerializeField] private float _defaultCorrectness;
    //[SerializeField] private ItemAudioHelper.Data _audioData;
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private bool _isLobShot;
    [SerializeField] private bool _isDynamicLobShot;
    [SerializeField] private bool _isAnswer;
    [SerializeField] private DestroyData[] _destroyData;
    [SerializeField] private bool _isPlane;

    private Tween _confiscateTween;
    private Sequence _warningSequence;
    private bool _warningStarted;
    private Action _onConfiscationFreed;

    private bool _hasBeenThrown;
    private bool _hasDropped;
    private float _thrownTime;
    public float ThrownTime => _thrownTime;

    public bool IsLobShot => _isLobShot;
    public bool IsDynamicLobShot => _isDynamicLobShot;
    public bool IsPlane => _isPlane;


    //private ItemAudioHelper _audioHelper;
    private string _answerID;
    private float _correctness;
    private string _contributorActorID;
    private float? _remainingTimeToDestroyOnIdle;
    private EState _state;
    private EIdleSource _idleSource;
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
    public bool IsBeingHeld => _state == EState.BeingHeld;
    public bool IsConfiscated => _state == EState.Confiscated;

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
        if (_state == EState.Undefined)
        {
            SetIdleState(EIdleSource.FromStart);
        }
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

        if (_state == EState.Idle && _remainingTimeToDestroyOnIdle.HasValue)
        {
            _remainingTimeToDestroyOnIdle -= Time.deltaTime;
            if (_remainingTimeToDestroyOnIdle <= 0f)
            {
                Destroy(gameObject);
            }
            else if (!_warningStarted && _remainingTimeToDestroyOnIdle <= _warningTime)
            {
                _warningStarted = true;
                StartWarning();
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
                SetIdleState(EIdleSource.FromMidAir);
            }
        }
    }

    private void SetIdleState(EIdleSource source)
    {
        StopWarning();
        _state = EState.Idle;
        DestroyData destroyData = Array.Find(_destroyData, x => x.Type == source);
        _remainingTimeToDestroyOnIdle = destroyData != null ? destroyData.Delay : null;
        _idleSource = source;
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
        StopWarning();
        _onConfiscationFreed?.Invoke();
        _onConfiscationFreed = null;
        _ownerID = actorID;
        _state = EState.BeingHeld;
        _hasBeenThrown = false;
        StopAllCoroutines(); // Cancel any pending DestroyAfterDelay
        SetCollidersEnabled(false);
    }
    void IPickUpInteractionOwner.OnDropped()
    {
        _lastOwnerID = _ownerID;
        _ownerID = null;
        SetCollidersEnabled(true);
        SetIdleState(EIdleSource.FromDrop);
    }
    void IPickUpInteractionOwner.OnThrowed()
    {
        StopWarning();
        _hasBeenThrown = true;
        _hasDropped = false;
        _lastOwnerID = _ownerID;
        _ownerID = null;
        _state = EState.MidAir;
        _hasHitGround = false;
        _thrownTime = Time.time;
        SetCollidersEnabled(true);
    }

    private void StartWarning()
    {
        _warningSequence = Sequence.Create(cycles: -1)
            .Chain(Tween.Scale(_rendererContainer, Vector3.one * _warningPulseScale, _warningPulseDuration, Ease.OutQuad))
            .Chain(Tween.Scale(_rendererContainer, Vector3.one, _warningPulseDuration, Ease.InQuad));
    }

    private void StopWarning()
    {
        _warningSequence.Stop();
        _warningStarted = false;
        if (_rendererContainer != null)
            _rendererContainer.localScale = Vector3.one;
    }

    public void Confiscate(Transform point, Action onFreed)
    {
        if (_state == EState.Confiscated) return;

        _confiscateTween.Stop();
        StopWarning();
        _onConfiscationFreed = onFreed;
        _state = EState.Confiscated;

        Vector3 spawnPosition = transform.position;

        _confiscateTween = Tween.Scale(_rendererContainer, Vector3.zero, _confiscateShrinkDuration, Ease.InBack)
            .OnComplete(() =>
            {
                if (_confiscateVFXPrefab != null)
                {
                    ParticleSystem cloud = Instantiate(_confiscateVFXPrefab, spawnPosition, Quaternion.identity);
                    Destroy(cloud.gameObject, cloud.main.duration + cloud.main.startLifetime.constantMax);
                }

                transform.position = point.position;
                if (TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                _rendererContainer.localScale = Vector3.one;
            });
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    public bool HasBeenThrown()
    {
        return _state == EState.MidAir || _state == EState.Idle;
    }
}

using UnityEngine;

public class PlayerController : MonoBehaviour, IInteractionActor, IThrowActor
{
    [SerializeField] private PlayerView _view;
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerPhysics _physics;

    [Header("Data")]
    [SerializeField] private InteractionHelper.Data _interactionData;
    [SerializeField] private ThrowHelper.Data _throwData;
    [SerializeField] private float _lookSpeed = 1080f; // Degrees per second
    [SerializeField] private float _dashCooldown = 0.2f; // Seconds
    [SerializeField] private float _hardStunDuration = 1f;
    [SerializeField] private float _softStunDuration = 0.5f;
    [Tag]
    [SerializeField] private string[] _hardCollisionTags;
    [Header("TO BE REMOVED")]
    [SerializeField] private bool _dropByHoldingInteract; // Once we decide on the final input scheme, this can be removed

    // Look
    private Vector3 _lookDirection;
    private Transform _lookAtPoint;
    // Timers
    private float _dashCooldownTimer;
    private float _stunTimer;
    // States
    private bool _isStunned;
    // Helpers
    private InteractionHelper _interactionHelper;
    private ThrowHelper _throwHelper;
    private DeskHelper _deskHelper;
    // IInteractionActor
    Vector3 IInteractionActor.Position => transform.position;
    Vector3 IInteractionActor.Forward => transform.forward;
    // IThrowActor
    Vector3 IThrowActor.LookDirection => _lookDirection;
    Collider IThrowActor.Collider => _physics.Collider;

    private void Awake()
    {
        _physics.Initialize();
        _lookDirection = transform.forward;
        _interactionHelper = new InteractionHelper(this, _interactionData);
        _throwHelper = new ThrowHelper(this, _throwData, _interactionHelper);
        _deskHelper = new DeskHelper(_inputHandler, _view, _physics);
    }

    private void OnEnable()
    {
        _inputHandler.ActionEvent += OnActionRequested;
        _inputHandler.DirectionalActionEvent += OnDirectionalActionRequested;
        _inputHandler.PreHoldActionEvent += OnPreHoldActionDetected;
        _inputHandler.HoldActionEvent += OnHoldActionRequested;
    }

    private void OnDisable()
    {
        _inputHandler.ActionEvent -= OnActionRequested;
        _inputHandler.DirectionalActionEvent -= OnDirectionalActionRequested;
        _inputHandler.PreHoldActionEvent -= OnPreHoldActionDetected;
        _inputHandler.HoldActionEvent -= OnHoldActionRequested;
    }

    private void OnActionRequested(EAction actionType)
    {
        if (actionType == EAction.Dash)
        {
            if (_dashCooldownTimer <= 0)
            {
                _view.OnStartDash();
                _physics.StartDashing(_lookDirection);
                _dashCooldownTimer = _dashCooldown;
            }
        }
        else if (actionType == EAction.Interact)
        {
            if (_interactionHelper.TryStartInteraction(out InteractionController interaction))
            {
                if (interaction.Type == EInteraction.PickUp)
                {
                    _view.OnPickUp(interaction.transform);
                }
                else if (interaction.Type == EInteraction.Static)
                {
                    DeskController deskController = interaction.GetComponent<DeskController>();
                    if (deskController != null)
                    {
                        _lookAtPoint = deskController.LookAtPoint;
                        _deskHelper.StartSitting(deskController);
                    }
                    else
                    {
                        Debug.LogError("Static interaction is not being handled: " + interaction.name);
                    }
                }
            }
        }
        else if (actionType == EAction.Action)
        {
            if (!_dropByHoldingInteract)
            {
                RestoreScope();
                TryDropItem();
            }
        }
        else if (actionType == EAction.Cancel)
        {
            if (_inputHandler.ScopeType == EInputScope.PlayerAiming)
            {
                RestoreScope();
                _inputHandler.CancelActionHold();
            }
            else if (_inputHandler.ScopeType == EInputScope.PlayerSitting)
            {
                _lookAtPoint = null;
                _deskHelper.StartStanding();
            }
        }
    }

    private void RestoreScope()
    {
        if (_deskHelper.IsSitting)
        {
            _lookAtPoint = _deskHelper.LookAtPoint;
            _inputHandler.SetScope(EInputScope.PlayerSitting);
        }
        else
        {
            _inputHandler.SetScope(EInputScope.PlayerStanding);
        }
    }

    private void OnDirectionalActionRequested(EDirectionalAction actionType, Vector2 input)
    {
        if (actionType == EDirectionalAction.Move)
        {
            _physics.SetMoveDirection(new Vector3(input.x, 0, input.y));
            if (input != Vector2.zero)
            {
                _lookDirection = new Vector3(input.x, 0, input.y);
            }
        }
        else if (actionType == EDirectionalAction.Aim)
        {
            if (input != Vector2.zero)
            {
                _lookDirection = new Vector3(input.x, 0, input.y);
            }
        }
    }

    private void OnPreHoldActionDetected(EAction actionType)
    {
        if (actionType == EAction.Action && _interactionHelper.TryGetPickedUpInteraction(out _))
        {
            _lookAtPoint = null;
            _inputHandler.SetScope(EInputScope.PlayerAiming);
        }
    }

    private void OnHoldActionRequested(EAction actionType, bool isHolding)
    {
        if (actionType == EAction.Interact && _dropByHoldingInteract)
        {
            TryDropItem();
        }
        else if (actionType == EAction.Action && !isHolding)
        {
            RestoreScope();
            _throwHelper.TryTriggerThrow();
        }
    }

    private void TryDropItem()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
            _view.OnDrop(stoppedInteraction.transform);
        }
    }

    private void Update()
    {
        _dashCooldownTimer -= Time.deltaTime;

        if (_isStunned)
        {
            UpdateStun();
            return;
        }

        if (_lookAtPoint != null)
        {
            Vector3 lookPosition = _lookAtPoint.position;
            lookPosition.y = transform.position.y;
            _lookDirection = (lookPosition - transform.position).normalized;
        }

        if (_lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
        }

        _interactionHelper.UpdateBestInteraction();
    }

    private void FixedUpdate()
    {
        _physics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_isStunned, out bool stoppedDashing);
        if (stoppedDashing)
        {
            _view.OnStopDash();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (_physics.IsFrontalCollision(contact.normal))
            {
                _physics.ClearCollisionNormals();
                if (_physics.TryStopDashing())
                {
                    _view.OnStopDash();
                    bool isSoftStun = !HasAnyTag(collision.transform, _hardCollisionTags);
                    StartStun(isSoftStun);
                }
                return;
            }

            _physics.AddCollisionNormal(contact.normal);
        }
    }

    private void OnTriggerEnter(Collider other)
        => _interactionHelper.OnTriggerEnter(other);

    private void OnTriggerExit(Collider other)
        => _interactionHelper.OnTriggerExit(other);

    void IThrowActor.OnThrow(Transform thrownTransform)
        => _view.OnThrow(thrownTransform);

    private void StartStun(bool isSoftStun)
    {
        _stunTimer = isSoftStun ? _softStunDuration : _hardStunDuration;
        _isStunned = true;
        _view.OnStartStun(isSoftStun);
    }

    private void UpdateStun()
    {
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0)
        {
            _isStunned = false;
            _view.OnStopStun();
        }
    }

    private bool HasAnyTag(Transform target, string[] tags)
    {
        foreach (var tag in tags)
        {
            if (target.CompareTag(tag)) return true;
        }
        return false;
    }
}

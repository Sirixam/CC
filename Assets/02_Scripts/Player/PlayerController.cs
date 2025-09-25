using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerView _view;
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerPhysics _playerPhysics;

    [Header("Configurations")]
    [SerializeField] private float _lookSpeed = 1080f; // Degrees per second
    [SerializeField] private float _dashCooldown = 0.2f; // Seconds
    [SerializeField] private float _hardStunDuration = 1f;
    [SerializeField] private float _softStunDuration = 0.5f;
    [Header("Tags")]
    [Tag]
    [SerializeField] private string[] _hardCollisionTags;
    [Tag]
    [SerializeField] private string[] _interactionTags;

    // Look
    private Vector3 _lookDirection;
    // Timers
    private float _dashCooldownTimer;
    private float _stunTimer;
    // States
    private bool _isStunned;
    // Helpers
    private InteractionHelper _interactionHelper = new();

    private void Awake()
    {
        _playerPhysics.Initialize();
        _lookDirection = transform.forward;
    }

    private void OnEnable()
    {
        _inputHandler.ActionEvent += OnActionRequested;
        _inputHandler.DirectionalActionEvent += OnDirectionalActionRequested;
        _inputHandler.HoldActionEvent += OnHoldActionRequested;
    }

    private void OnDisable()
    {
        _inputHandler.ActionEvent -= OnActionRequested;
        _inputHandler.DirectionalActionEvent -= OnDirectionalActionRequested;
        _inputHandler.HoldActionEvent -= OnHoldActionRequested;
    }

    private void OnActionRequested(EAction actionType)
    {
        if (actionType == EAction.Dash)
        {
            if (_dashCooldownTimer <= 0)
            {
                _view.OnStartDash();
                _playerPhysics.StartDashing(_lookDirection);
                _dashCooldownTimer = _dashCooldown;
            }
        }
        else if (actionType == EAction.Interact)
        {
            if (_interactionHelper.TryStartInteraction(out InteractionController interaction) && interaction.Type == EInteraction.PickUp)
            {
                _view.OnPickUp(interaction.transform);
            }
        }
    }

    private void OnDirectionalActionRequested(EDirectionalAction actionType, Vector2 input)
    {
        if (actionType == EDirectionalAction.Move)
        {
            _playerPhysics.SetMoveDirection(new Vector3(input.x, 0, input.y));
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

    private void OnHoldActionRequested(EAction actionType, bool isHolding)
    {
        if (actionType == EAction.Interact)
        {
            if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
            {
                _interactionHelper.TryStopInteraction(stoppedInteraction);
                _view.OnDrop(stoppedInteraction.transform);
            }
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

        if (_lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        _playerPhysics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_isStunned, out bool stoppedDashing);
        if (stoppedDashing)
        {
            _view.OnStopDash();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (_playerPhysics.IsFrontalCollision(contact.normal))
            {
                _playerPhysics.ClearCollisionNormals();
                if (_playerPhysics.TryStopDashing())
                {
                    _view.OnStopDash();
                    bool isSoftStun = !HasAnyTag(collision.transform, _hardCollisionTags);
                    StartStun(isSoftStun);
                }
                return;
            }

            _playerPhysics.AddCollisionNormal(contact.normal);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasAnyTag(other.transform, _interactionTags))
        {
            var interaction = other.GetComponentInParent<InteractionController>();
            if (interaction == null)
            {
                Debug.LogError("Interaction controller was not found in object tagged as interaction: " + other.transform.name);
                return;
            }
            _interactionHelper.AddInteraction(interaction);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (HasAnyTag(other.transform, _interactionTags))
        {
            var interaction = other.GetComponentInParent<InteractionController>();
            if (interaction == null)
            {
                Debug.LogError("Interaction controller was not found in object tagged as interaction: " + other.transform.name);
                return;
            }
            _interactionHelper.RemoveInteraction(interaction);
        }
    }

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

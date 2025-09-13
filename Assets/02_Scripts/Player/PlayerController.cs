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

    // Look
    private Vector3 _lookDirection;
    // Timers
    private float _dashCooldownTimer;
    private float _stunTimer;
    // States
    private bool _isStunned;

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
                _playerPhysics.StartDashing(_lookDirection);
                _dashCooldownTimer = _dashCooldown;
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
        _playerPhysics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_isStunned);
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
                    bool isSoftStun = !collision.transform.CompareTag("Environment");
                    StartStun(isSoftStun);
                }
                return;
            }

            _playerPhysics.AddCollisionNormal(contact.normal);
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
}

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerPhysics _playerPhysics;

    [Header("Configurations")]
    [SerializeField] private float _lookSpeed = 1080f; // Degrees per second
    [SerializeField] private float _dashCooldown = 0.2f; // Seconds

    // Look
    private Vector3 _lookDirection;
    // Cooldowns
    private float _dashCooldownTimer;

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
        if (_lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
        }

        _dashCooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        _playerPhysics.OnFixedUpdate(Time.fixedDeltaTime);
    }
}

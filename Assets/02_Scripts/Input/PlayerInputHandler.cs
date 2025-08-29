using UnityEngine;
using UnityEngine.InputSystem;

public enum EInputScope
{
    Undefined,
    Menu,
    PlayerStanding,
    PlayerSitting,
}

public class PlayerInputHandler : MonoBehaviour
{
    private struct HoldAction
    {
        private const float HOLD_THRESHOLD = 0.3f; // Time in seconds to consider "hold" instead of "tap"

        private float _startTime;
        private bool _isInputStarted;

        public bool IsHolding;

        public void OnStarted()
        {
            _startTime = Time.time;
            _isInputStarted = true;
            IsHolding = false;
        }

        public void OnUpdate(out bool beginHold)
        {
            beginHold = false;
            if (!_isInputStarted || IsHolding) return;

            if (Time.time - _startTime >= HOLD_THRESHOLD)
            {
                beginHold = true;
                IsHolding = true;
            }
        }

        public void OnCanceled(out bool wasHolding)
        {
            wasHolding = IsHolding;
            _isInputStarted = false;
            IsHolding = false;
        }
    }

    private const string PLAYER_STANDING_MAP = "Player - Standing";
    private const string PLAYER_SITTING_MAP = "Player - Sitting";
    private const string MENU_MAP = "Menu";

    private const string MOVE_ACTION = "Move";
    private const string NAVIGATE_ACTION = "Navigate";
    private const string ACTION_ACTION = "Action";
    private const string INTERACT_ACTION = "Interact";
    private const string DASH_ACTION = "Dash";
    private const string UTILITY_ACTION = "Utility";
    private const string CANCEL_ACTION = "Cancel";
    private const string PAUSE_ACTION = "Pause";

    private PlayerInput _playerInput;
    [SerializeField] private EInputScope _scopeType;

    private HoldAction _actionHoldState;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        foreach (var actionMap in _playerInput.actions.actionMaps)
        {
            actionMap.Disable();
        }

        Debug.Log("Current control scheme: " + _playerInput.currentControlScheme);
    }

    private void OnEnable()
    {
        if (_scopeType != EInputScope.Undefined)
        {
            SubscribeActions(_scopeType);
        }
    }

    private void OnDisable()
    {
        if (_scopeType != EInputScope.Undefined)
        {
            UnsubscribeActions(_scopeType);
        }
    }

    private void Update()
    {
        _actionHoldState.OnUpdate(out bool beginHold);
        if (beginHold)
        {
            Debug.Log("Action hold begin");
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (context.performed)
        {
            Debug.Log("Move requested with input: " + input);
        }
        else if (context.canceled)
        {
            Debug.Log("Move canceled");
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (context.performed)
        {
            Debug.Log("Navigate requested with input: " + input);
        }
        else if (context.canceled)
        {
            Debug.Log("Navigate canceled");
        }
    }

    private void OnAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _actionHoldState.OnStarted();
        }
        else if (context.canceled)
        {
            _actionHoldState.OnCanceled(out bool wasHolding);
            if (wasHolding)
            {
                Debug.Log("Action hold canceled");
            }
            else
            {
                Debug.Log("Action requested");
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("Interact requested");
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        Debug.Log("Dash requested");
    }

    private void OnUtility(InputAction.CallbackContext context)
    {
        Debug.Log("Utility requested");
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        Debug.Log("Cancel requested");
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        Debug.Log("Pause requested");
    }

    #region SCOPE

    public void SetScope(EInputScope scopeType)
    {
        UnsubscribeActions(_scopeType);
        _scopeType = scopeType;
        SubscribeActions(scopeType);
    }

    private void UnsubscribeActions(EInputScope scopeType)
    {
        switch (scopeType)
        {
            case EInputScope.Menu: UnsubscribeMenuActions(); break;
            case EInputScope.PlayerStanding: UnsubscribePlayerStandingActions(); break;
            case EInputScope.PlayerSitting: UnsubscribePlayerSittingActions(); break;
            case EInputScope.Undefined: break;
            default:
                Debug.LogError("Scope is not being handled: " + scopeType);
                break;
        }
    }

    private void SubscribeActions(EInputScope scopeType)
    {
        switch (scopeType)
        {
            case EInputScope.Menu: SubscribeMenuActions(); break;
            case EInputScope.PlayerStanding: SubscribePlayerStandingActions(); break;
            case EInputScope.PlayerSitting: SubscribePlayerSittingActions(); break;
            default:
                Debug.LogError("Scope is not being handled: " + scopeType);
                break;
        }
    }

    private void SubscribePlayerStandingActions()
    {
        _playerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Enable();

        var actions = _playerInput.actions;
        actions[MOVE_ACTION].started += OnMove;
        actions[MOVE_ACTION].performed += OnMove;
        actions[MOVE_ACTION].canceled += OnMove;

        actions[ACTION_ACTION].started += OnAction;
        actions[ACTION_ACTION].performed += OnAction;
        actions[ACTION_ACTION].canceled += OnAction;

        actions[INTERACT_ACTION].performed += OnInteract;
        actions[DASH_ACTION].performed += OnDash;
        actions[PAUSE_ACTION].performed += OnPause;
    }

    private void UnsubscribePlayerStandingActions()
    {
        _playerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Disable();

        var actions = _playerInput.actions;
        actions[MOVE_ACTION].started -= OnMove;
        actions[MOVE_ACTION].performed -= OnMove;
        actions[MOVE_ACTION].canceled -= OnMove;

        actions[ACTION_ACTION].started -= OnAction;
        actions[ACTION_ACTION].canceled -= OnAction;

        actions[INTERACT_ACTION].performed -= OnInteract;
        actions[DASH_ACTION].performed -= OnDash;
        actions[PAUSE_ACTION].performed -= OnPause;
    }

    private void SubscribePlayerSittingActions()
    {
        _playerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Enable();

        var actions = _playerInput.actions;
        actions[NAVIGATE_ACTION].performed += OnNavigate;
        actions[NAVIGATE_ACTION].canceled += OnNavigate;

        actions[ACTION_ACTION].started += OnAction;
        actions[ACTION_ACTION].canceled += OnAction;

        actions[INTERACT_ACTION].performed += OnInteract;
        actions[UTILITY_ACTION].performed += OnUtility;
        actions[CANCEL_ACTION].performed += OnCancel;
        actions[PAUSE_ACTION].performed += OnPause;
    }

    private void UnsubscribePlayerSittingActions()
    {
        _playerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Disable();

        var actions = _playerInput.actions;
        actions[NAVIGATE_ACTION].performed -= OnNavigate;
        actions[NAVIGATE_ACTION].canceled -= OnNavigate;

        actions[ACTION_ACTION].started -= OnAction;
        actions[ACTION_ACTION].canceled -= OnAction;

        actions[INTERACT_ACTION].performed -= OnInteract;
        actions[UTILITY_ACTION].performed -= OnUtility;
        actions[CANCEL_ACTION].performed -= OnCancel;
        actions[PAUSE_ACTION].performed -= OnPause;
    }

    private void SubscribeMenuActions()
    {
        _playerInput.actions.FindActionMap(MENU_MAP).Enable();

        var actions = _playerInput.actions;
        actions[NAVIGATE_ACTION].performed += OnNavigate;
        actions[NAVIGATE_ACTION].canceled += OnNavigate;

        // TODO
    }

    private void UnsubscribeMenuActions()
    {
        _playerInput.actions.FindActionMap(MENU_MAP).Disable();

        var actions = _playerInput.actions;
        actions[NAVIGATE_ACTION].performed -= OnNavigate;
        actions[NAVIGATE_ACTION].canceled -= OnNavigate;

        // TODO
    }

    #endregion
}

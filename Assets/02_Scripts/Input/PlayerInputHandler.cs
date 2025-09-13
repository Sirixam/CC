#define LOG_ACTIONS

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EInputScope
{
    Undefined,
    Menu,
    PlayerStanding,
    PlayerSitting,
    PlayerAiming,
}

public enum EDirectionalAction
{
    Move,
    Navigate,
    Aim
}

public enum EAction
{
    Action,
    Interact,
    Dash,
    Utility,
    Cancel,
    Pause,
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
    private const string PLAYER_AIMING_MAP = "Player - Aiming";
    //private const string MENU_MAP = "Menu";

    private static readonly string AIM_ACTION = EDirectionalAction.Aim.ToString();
    private static readonly string MOVE_ACTION = EDirectionalAction.Move.ToString();
    private static readonly string NAVIGATE_ACTION = EDirectionalAction.Navigate.ToString();
    private static readonly string ACTION_ACTION = EAction.Action.ToString();
    private static readonly string INTERACT_ACTION = EAction.Interact.ToString();
    private static readonly string DASH_ACTION = EAction.Dash.ToString();
    private static readonly string UTILITY_ACTION = EAction.Utility.ToString();
    private static readonly string CANCEL_ACTION = EAction.Cancel.ToString();
    private static readonly string PAUSE_ACTION = EAction.Pause.ToString();

    private HoldAction _actionHoldState;
    private HoldAction _interactHoldState;

    public PlayerInput PlayerInput { get; private set; }
    public EInputScope ScopeType { get; private set; }
    public bool IsHoldingAction => _actionHoldState.IsHolding;
    public bool IsHoldingInteract => _interactHoldState.IsHolding;

    public Action<EAction> ActionEvent;
    public Action<EAction, bool> HoldActionEvent;
    public Action<EDirectionalAction, Vector2> DirectionalActionEvent;

    public void Initialize()
    {
        PlayerInput = GetComponent<PlayerInput>();

        // DO NOT iterate over all action maps, because we want to keep some enabled, eg UI
        PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Disable();
        PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Disable();
        PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Disable();
        //_playerInput.actions.FindActionMap(MENU_MAP).Disable();

        Debug.Log("Current control scheme: " + PlayerInput.currentControlScheme);
    }

    private void OnEnable()
    {
        if (ScopeType != EInputScope.Undefined)
        {
            SubscribeActions(ScopeType);
        }
    }

    private void OnDisable()
    {
        if (ScopeType != EInputScope.Undefined)
        {
            UnsubscribeActions(ScopeType);
        }
    }

    private void Update()
    {
        _actionHoldState.OnUpdate(out bool beginHoldAction);
        if (beginHoldAction)
        {
            RequestHoldAction(EAction.Action, true);
        }

        _interactHoldState.OnUpdate(out bool beginHoldInteract);
        if (beginHoldInteract)
        {
            RequestHoldAction(EAction.Interact, true);
        }
    }

    public void SetScope(EInputScope scopeType)
    {
        UnsubscribeActions(ScopeType);
        ScopeType = scopeType;
        SubscribeActions(scopeType);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            RequestDirectionalAction(EDirectionalAction.Move, input);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(EDirectionalAction.Move, Vector2.zero);
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            RequestDirectionalAction(EDirectionalAction.Navigate, input);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(EDirectionalAction.Navigate, Vector2.zero);
        }
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            RequestDirectionalAction(EDirectionalAction.Aim, input);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(EDirectionalAction.Aim, Vector2.zero);
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
                RequestHoldAction(EAction.Action, false);
            }
            else
            {
                RequestAction(EAction.Action);
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _interactHoldState.OnStarted();
        }
        else if (context.canceled)
        {
            _interactHoldState.OnCanceled(out bool wasHolding);
            if (wasHolding)
            {
                RequestHoldAction(EAction.Interact, false);
            }
            else
            {
                RequestAction(EAction.Interact);
            }
        }
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Dash);
    }

    private void OnUtility(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Utility);
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Cancel);
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Pause);
    }

    private void RequestDirectionalAction(EDirectionalAction actionType, Vector2 input)
    {
        DirectionalActionEvent?.Invoke(actionType, input);
#if LOG_ACTIONS
        Debug.Log($"{actionType} requested with input: {input}");
#endif
    }

    private void RequestHoldAction(EAction actionType, bool isHolding)
    {
        HoldActionEvent?.Invoke(actionType, isHolding);
#if LOG_ACTIONS
        if (isHolding)
        {
            Debug.Log($"{actionType} hold begin");
        }
        else
        {
            Debug.Log($"{actionType} hold end");
        }
#endif
    }

    private void RequestAction(EAction actionType)
    {
        ActionEvent?.Invoke(actionType);
#if LOG_ACTIONS
        Debug.Log($"{actionType} requested");
#endif
    }

    #region SCOPE

    private void UnsubscribeActions(EInputScope scopeType)
    {
        switch (scopeType)
        {
            case EInputScope.Menu: UnsubscribeMenuActions(); break;
            case EInputScope.PlayerStanding: UnsubscribePlayerStandingActions(); break;
            case EInputScope.PlayerSitting: UnsubscribePlayerSittingActions(); break;
            case EInputScope.PlayerAiming: UnsubscribePlayerAimingActions(); break;
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
            case EInputScope.PlayerAiming: SubscribePlayerAimingActions(); break;
            default:
                Debug.LogError("Scope is not being handled: " + scopeType);
                break;
        }
    }

    private void SubscribePlayerStandingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Enable();

        var actions = PlayerInput.actions;
        actions[MOVE_ACTION].started += OnMove;
        actions[MOVE_ACTION].performed += OnMove;
        actions[MOVE_ACTION].canceled += OnMove;

        actions[ACTION_ACTION].started += OnAction;
        actions[ACTION_ACTION].performed += OnAction;
        actions[ACTION_ACTION].canceled += OnAction;

        actions[INTERACT_ACTION].started += OnInteract;
        actions[INTERACT_ACTION].performed += OnInteract;
        actions[INTERACT_ACTION].canceled += OnInteract;

        actions[DASH_ACTION].performed += OnDash;
        actions[PAUSE_ACTION].performed += OnPause;
    }

    private void UnsubscribePlayerStandingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Disable();

        var actions = PlayerInput.actions;
        actions[MOVE_ACTION].started -= OnMove;
        actions[MOVE_ACTION].performed -= OnMove;
        actions[MOVE_ACTION].canceled -= OnMove;

        actions[ACTION_ACTION].started -= OnAction;
        actions[ACTION_ACTION].performed -= OnAction;
        actions[ACTION_ACTION].canceled -= OnAction;

        actions[INTERACT_ACTION].started -= OnInteract;
        actions[INTERACT_ACTION].performed -= OnInteract;
        actions[INTERACT_ACTION].canceled -= OnInteract;

        actions[DASH_ACTION].performed -= OnDash;
        actions[PAUSE_ACTION].performed -= OnPause;
    }

    private void SubscribePlayerSittingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Enable();

        var actions = PlayerInput.actions;
        actions[NAVIGATE_ACTION].performed += OnNavigate;
        actions[NAVIGATE_ACTION].canceled += OnNavigate;

        actions[ACTION_ACTION].started += OnAction;
        actions[ACTION_ACTION].performed += OnAction;
        actions[ACTION_ACTION].canceled += OnAction;

        actions[INTERACT_ACTION].performed += OnInteract;
        actions[UTILITY_ACTION].performed += OnUtility;
        actions[CANCEL_ACTION].performed += OnCancel;
        actions[PAUSE_ACTION].performed += OnPause;
    }

    private void UnsubscribePlayerSittingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Disable();

        var actions = PlayerInput.actions;
        actions[NAVIGATE_ACTION].performed -= OnNavigate;
        actions[NAVIGATE_ACTION].canceled -= OnNavigate;

        actions[ACTION_ACTION].started -= OnAction;
        actions[ACTION_ACTION].performed -= OnAction;
        actions[ACTION_ACTION].canceled -= OnAction;

        actions[INTERACT_ACTION].performed -= OnInteract;
        actions[UTILITY_ACTION].performed -= OnUtility;
        actions[CANCEL_ACTION].performed -= OnCancel;
        actions[PAUSE_ACTION].performed -= OnPause;
    }

    private void SubscribePlayerAimingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Enable();

        var actions = PlayerInput.actions;
        actions[AIM_ACTION].performed += OnAim;
        actions[AIM_ACTION].canceled += OnAim;

        actions[ACTION_ACTION].started += OnAction;
        actions[ACTION_ACTION].canceled += OnAction;

        actions[CANCEL_ACTION].performed += OnCancel;
        actions[PAUSE_ACTION].performed += OnPause;
    }

    private void UnsubscribePlayerAimingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Disable();

        var actions = PlayerInput.actions;
        actions[AIM_ACTION].performed -= OnAim;
        actions[AIM_ACTION].canceled -= OnAim;

        actions[ACTION_ACTION].started -= OnAction;
        actions[ACTION_ACTION].canceled -= OnAction;

        actions[CANCEL_ACTION].performed -= OnCancel;
        actions[PAUSE_ACTION].performed -= OnPause;
    }

    private void SubscribeMenuActions()
    {
        //_playerInput.actions.FindActionMap(MENU_MAP).Enable();

        //var actions = _playerInput.actions;
        //actions[NAVIGATE_ACTION].performed += OnNavigate;
        //actions[NAVIGATE_ACTION].canceled += OnNavigate;

        // TODO
    }

    private void UnsubscribeMenuActions()
    {
        //_playerInput.actions.FindActionMap(MENU_MAP).Disable();

        //var actions = _playerInput.actions;
        //actions[NAVIGATE_ACTION].performed -= OnNavigate;
        //actions[NAVIGATE_ACTION].canceled -= OnNavigate;

        // TODO
    }

    #endregion
}

#define LOG_ACTIONS

using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EInputScope
{
    Undefined,
    Menu,
    PlayerStanding,
    PlayerSitting,
    PlayerAiming,
    PlayerPeeking,
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
    Peek,
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
        private bool _waitingToReleaseInput;

        public bool IsHolding { get; private set; }

        public void OnPressInput(out bool canProcessInput)
        {
            canProcessInput = !_isInputStarted && !_waitingToReleaseInput;
            if (!canProcessInput) return;

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

        public void OnReleaseInput(out bool wasHolding, out bool canProcessInput)
        {
            canProcessInput = _isInputStarted;
            wasHolding = IsHolding;
            _isInputStarted = false;
            IsHolding = false;
            _waitingToReleaseInput = false;
        }

        public void Cancel()
        {
            _waitingToReleaseInput = _isInputStarted;
            _isInputStarted = false;
            IsHolding = false;
        }
    }

    private const string PLAYER_STANDING_MAP = "Player - Standing";
    private const string PLAYER_SITTING_MAP = "Player - Sitting";
    private const string PLAYER_AIMING_MAP = "Player - Aiming";
    private const string PLAYER_PEEKING_MAP = "Player - Peeking";
    //private const string MENU_MAP = "Menu";

    private static readonly string AIM_ACTION = EDirectionalAction.Aim.ToString();
    private static readonly string MOVE_ACTION = EDirectionalAction.Move.ToString();
    private static readonly string NAVIGATE_ACTION = EDirectionalAction.Navigate.ToString();
    private static readonly string ACTION_ACTION = EAction.Action.ToString();
    private static readonly string INTERACT_ACTION = EAction.Interact.ToString();
    private static readonly string DASH_ACTION = EAction.Dash.ToString();
    private static readonly string PEEK_ACTION = EAction.Peek.ToString();
    private static readonly string CANCEL_ACTION = EAction.Cancel.ToString();
    private static readonly string PAUSE_ACTION = EAction.Pause.ToString();

    [SerializeField] private bool _invertMovement;
    [SerializeField] private bool _invertAim;

    private HoldAction _actionHoldState;
    private HoldAction _peekHoldState;
    private HoldAction _interactHoldState;
    private EInputScope _nextScopeType; // Used when changing scope.

    public PlayerInput PlayerInput { get; private set; }
    public EInputScope ScopeType { get; private set; }
    public bool IsHoldingAction => _actionHoldState.IsHolding;
    public bool IsHoldingInteract => _interactHoldState.IsHolding;

    public Action<EAction> ActionEvent;
    public Action<EAction> PreHoldActionEvent;
    public Action<EAction, bool> HoldActionEvent;
    public Action<EDirectionalAction, Vector2> DirectionalActionEvent;

    public void Initialize()
    {
        PlayerInput = GetComponent<PlayerInput>();

        // DO NOT iterate over all action maps, because we want to keep some enabled, eg UI
        PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Disable();
        PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Disable();
        PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Disable();
        PlayerInput.actions.FindActionMap(PLAYER_PEEKING_MAP).Disable();
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

        _peekHoldState.OnUpdate(out bool beginHoldPeek);
        if (beginHoldPeek)
        {
            RequestHoldAction(EAction.Peek, true);
        }
    }

    public void SetScope(EInputScope scopeType)
    {
        if (ScopeType == scopeType) return;

        _nextScopeType = scopeType;
        UnsubscribeActions(ScopeType);
        ScopeType = scopeType;
        SubscribeActions(scopeType);
        _nextScopeType = EInputScope.Undefined;
    }

    public void CancelActionHold()
    {
        _actionHoldState.Cancel();
    }

    public void CancelPeekHold()
    {
        _peekHoldState.Cancel();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            if (_invertMovement) input *= -1;
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
            if (_invertAim) input *= -1;
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
            _actionHoldState.OnPressInput(out bool canProcessInput);
            if (!canProcessInput) return;

            PreHoldActionEvent?.Invoke(EAction.Action);
        }
        else if (context.canceled && (_nextScopeType == EInputScope.Undefined || !HasActionInput(_nextScopeType))) // Supress cancel when changing scope to one that has the action input too.
        {
            _actionHoldState.OnReleaseInput(out bool wasHolding, out bool canProcessInput);
            if (!canProcessInput) return;

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
            _interactHoldState.OnPressInput(out bool canProcessInput);
            if (!canProcessInput) return;

            PreHoldActionEvent?.Invoke(EAction.Interact);
        }
        else if (context.canceled)
        {
            _interactHoldState.OnReleaseInput(out bool wasHolding, out bool canProcessInput);
            if (!canProcessInput) return;

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

    private void OnPeek(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _peekHoldState.OnPressInput(out bool canProcessInput);
            if (!canProcessInput) return;

            PreHoldActionEvent?.Invoke(EAction.Peek);
        }
        else if (context.canceled && (_nextScopeType == EInputScope.Undefined || !HasPeekInput(_nextScopeType))) // Supress cancel when changing scope to one that has the action input too.
        {
            _peekHoldState.OnReleaseInput(out bool wasHolding, out bool canProcessInput);
            if (!canProcessInput) return;

            if (wasHolding)
            {
                RequestHoldAction(EAction.Peek, false);
            }
            else
            {
                RequestAction(EAction.Peek);
            }
        }
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

    // EXTENDABLE
    private bool HasActionInput(EInputScope scopeType)
    {
        return scopeType == EInputScope.PlayerStanding || scopeType == EInputScope.PlayerSitting || scopeType == EInputScope.PlayerAiming;
    }

    private bool HasPeekInput(EInputScope scopeType)
    {
        return scopeType == EInputScope.PlayerStanding || scopeType == EInputScope.PlayerPeeking;
    }

    private void UnsubscribeActions(EInputScope scopeType)
    {
        switch (scopeType)
        {
            case EInputScope.Menu: UnsubscribeMenuActions(); break;
            case EInputScope.PlayerStanding: UnsubscribePlayerStandingActions(); break;
            case EInputScope.PlayerSitting: UnsubscribePlayerSittingActions(); break;
            case EInputScope.PlayerAiming: UnsubscribePlayerAimingActions(); break;
            case EInputScope.PlayerPeeking: UnsubscribePlayerPeekingActions(); break;
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
            case EInputScope.PlayerPeeking: SubscribePlayerPeekingActions(); break;
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

        actions[PEEK_ACTION].started += OnPeek;
        actions[PEEK_ACTION].performed += OnPeek;
        actions[PEEK_ACTION].canceled += OnPeek;

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

        actions[PEEK_ACTION].started -= OnPeek;
        actions[PEEK_ACTION].performed -= OnPeek;
        actions[PEEK_ACTION].canceled -= OnPeek;

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

        actions[INTERACT_ACTION].started += OnInteract;
        actions[INTERACT_ACTION].performed += OnInteract;
        actions[INTERACT_ACTION].canceled += OnInteract;

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

        actions[INTERACT_ACTION].started -= OnInteract;
        actions[INTERACT_ACTION].performed -= OnInteract;
        actions[INTERACT_ACTION].canceled -= OnInteract;

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

    private void SubscribePlayerPeekingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_PEEKING_MAP).Enable();

        var actions = PlayerInput.actions;
        actions[AIM_ACTION].performed += OnAim;
        actions[AIM_ACTION].canceled += OnAim;

        actions[PEEK_ACTION].started += OnPeek;
        actions[PEEK_ACTION].performed += OnPeek;
        actions[PEEK_ACTION].canceled += OnPeek;

        actions[DASH_ACTION].performed += OnDash;
        actions[PAUSE_ACTION].performed += OnPause;
    }

    private void UnsubscribePlayerPeekingActions()
    {
        PlayerInput.actions.FindActionMap(PLAYER_PEEKING_MAP).Disable();

        var actions = PlayerInput.actions;
        actions[AIM_ACTION].performed -= OnAim;
        actions[AIM_ACTION].canceled -= OnAim;

        actions[PEEK_ACTION].started -= OnPeek;
        actions[PEEK_ACTION].performed -= OnPeek;
        actions[PEEK_ACTION].canceled -= OnPeek;

        actions[DASH_ACTION].performed -= OnDash;
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

#define LOG_ACTIONS

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EDirectionalAction
{
    Move,
    Navigate,
    Aim,
    Aim_WithMouse,
}

public enum EAction
{
    Action,
    Interact,
    Dash,
    Peek,
    Cancel,
    Pause,
    Utility,
}

/// <summary>
/// This class handles what input the player is doing.
/// It support different type of actions: normal actions (tap), hold actions (press and hold) and directional actions (with a Vector2 input)
/// </summary>
public partial class PlayerInputHandler : MonoBehaviour
{
    private struct HoldState
    {
        private const float HOLD_THRESHOLD = 0.3f; // Time in seconds to consider "hold" instead of "tap"

        private float _startTime;
        private bool _pressed;
        private bool _waitingToReleaseInput;

        public bool IsHolding { get; private set; }

        public void OnPressInput(out bool canProcessInput)
        {
            canProcessInput = !_pressed && !_waitingToReleaseInput;
            if (!canProcessInput) return;

            _startTime = Time.time;
            _pressed = true;
            IsHolding = false;
        }

        public void OnUpdate(out bool beginHold)
        {
            beginHold = false;
            if (!_pressed || IsHolding) return;

            if (Time.time - _startTime >= HOLD_THRESHOLD)
            {
                beginHold = true;
                IsHolding = true;
            }
        }

        public void OnReleaseInput(out bool wasHolding, out bool canProcessInput)
        {
            canProcessInput = _pressed;
            wasHolding = IsHolding;
            _pressed = false;
            IsHolding = false;
            _waitingToReleaseInput = false;
        }

        public void Cancel()
        {
            _waitingToReleaseInput = _pressed;
            _pressed = false;
            IsHolding = false;
        }
    }

    [SerializeField] private bool _invertMovement;
    [SerializeField] private bool _invertAim;

    private HoldState _actionHoldState;
    private HoldState _peekHoldState;
    private HoldState _interactHoldState;
    private HoldState _utilityHoldState;

    private Mapper _mapper;
    public PlayerInput PlayerInput { get; private set; }
    public EInputScope ScopeType => _mapper.ScopeType;

    public Action<EAction> ActionEvent;
    public Action<EAction> PreHoldActionEvent;
    public Action<EAction, bool> HoldActionEvent;
    public Action<EDirectionalAction, Vector2> DirectionalActionEvent;

    public void Initialize()
    {
        PlayerInput = GetComponent<PlayerInput>();
        _mapper = new Mapper(this);
        Debug.Log("Current control scheme: " + PlayerInput.currentControlScheme);
    }

    private void OnEnable()
        => _mapper.OnEnable();

    private void OnDisable()
        => _mapper.OnDisable();

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

        _utilityHoldState.OnUpdate(out bool beginHoldUtility);
        if (beginHoldUtility)
        {
            RequestHoldAction(EAction.Utility, true);
        }
    }

    public void SetScope(EInputScope scopeType)
        => _mapper.SetScope(scopeType);

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
        bool isMouse = context.control.device is Mouse;
        EDirectionalAction actionType = isMouse ? EDirectionalAction.Aim_WithMouse : EDirectionalAction.Aim;
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            if (!isMouse && _invertAim)
            {
                input *= -1;
            }
            RequestDirectionalAction(actionType, input);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(actionType, Vector2.zero);
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
        else if (context.canceled && !_mapper.SupressCancelOnScopeChange(EAction.Action))
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
        else if (context.canceled && !_mapper.SupressCancelOnScopeChange(EAction.Peek))
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

    private void OnUtility(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _utilityHoldState.OnPressInput(out bool canProcessInput);
            if (!canProcessInput) return;

            PreHoldActionEvent?.Invoke(EAction.Utility);
        }
        else if (context.canceled)
        {
            _utilityHoldState.OnReleaseInput(out bool wasHolding, out bool canProcessInput);
            if (!canProcessInput) return;

            if (wasHolding)
            {
                RequestHoldAction(EAction.Utility, false);
            }
            else
            {
                RequestAction(EAction.Utility);
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
}

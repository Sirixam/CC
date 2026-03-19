//#define LOG_ACTIONS

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EDirectionalAction
{
    Move,
    Navigate,
    Aim,
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
    Help,
}

public enum EDevice
{
    Undefined,
    Gamepad,
    KeyboardAndMouse,
}

/// <summary>
/// This class handles what input the player is doing.
/// It support different type of actions: normal actions (tap), hold actions (press and hold) and directional actions (with a Vector2 input)
/// </summary>
public partial class PlayerInputHandler : MonoBehaviour
{
    private bool _isBlocked;

    public void Block() => _isBlocked = true;
    public void Unblock() => _isBlocked = false;
    private class HoldState
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

    private class DeviceManager
    {
        public EDevice LastKnownDeviceType { get; private set; }

        public void UpdateLastKnownDevice(InputDevice device)
        {
            LastKnownDeviceType = GetDeviceFromContext(device);
        }

        public EDevice GetDeviceFromContext(InputDevice device)
        {
            if (device == null)
            {
                return EDevice.Undefined;
            }

            if (device is Keyboard || device is Mouse)
            {
                return EDevice.KeyboardAndMouse;
            }
            if (device is Gamepad)
            {
                return EDevice.Gamepad;
            }
            Debug.LogError("Last device is not being handled: " + device.GetType());
            return EDevice.Undefined;
        }
    }

    [SerializeField] private bool _invertMovement;
    [SerializeField] private bool _invertAim;
    [SerializeField] private Mapper.Data _mapperData;

    private HoldState _actionHoldState = new();
    private HoldState _peekHoldState = new();
    private HoldState _interactHoldState = new();
    private HoldState _utilityHoldState = new();
    private HoldState _helpHoldState = new();

    private DeviceManager _deviceManager;
    private Mapper _mapper;
    public PlayerInput PlayerInput { get; private set; }
    public EInputScope ScopeType => _mapper.ScopeType;
    public EDevice LastKnownDeviceType => _deviceManager.LastKnownDeviceType;

    public Action<EAction> ActionEvent;
    public Action<EAction> PreHoldActionEvent;
    public Action<EAction, bool> HoldActionEvent;
    public Action<EDirectionalAction, Vector2, bool> DirectionalActionEvent;

    public void Initialize()
    {
        PlayerInput = GetComponent<PlayerInput>();
        _mapper = new Mapper(_mapperData, this);
        _deviceManager = new DeviceManager();
        Debug.Log("Current control scheme: " + PlayerInput.currentControlScheme);
    }

    private void OnEnable()
    {
        if (_mapper != null)
            _mapper.OnEnable();
    }

    private void OnDisable()
    {
        if (_mapper != null)
            _mapper.OnDisable();
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

        _utilityHoldState.OnUpdate(out bool beginHoldUtility);
        if (beginHoldUtility)
        {
            RequestHoldAction(EAction.Utility, true);
        }

        _helpHoldState.OnUpdate(out bool beginHoldHelp);
        if (beginHoldHelp)
        {
            RequestHoldAction(EAction.Help, true);
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
            RequestDirectionalAction(EDirectionalAction.Move, input, isMouse: false);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(EDirectionalAction.Move, Vector2.zero, isMouse: false);
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            RequestDirectionalAction(EDirectionalAction.Navigate, input, isMouse: false);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(EDirectionalAction.Navigate, Vector2.zero, isMouse: false);
        }
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        bool isMouse = context.control.device is Mouse;
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            if (!isMouse && _invertAim)
            {
                input *= -1;
            }
            RequestDirectionalAction(EDirectionalAction.Aim, input, isMouse);
        }
        else if (context.canceled)
        {
            RequestDirectionalAction(EDirectionalAction.Aim, Vector2.zero, isMouse);
        }
    }

    private void OnAction(InputAction.CallbackContext context)
    {
        HandleHoldAction(context, _actionHoldState, EAction.Action);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        HandleHoldAction(context, _interactHoldState, EAction.Interact);

    }

    private void OnDash(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Dash);
    }

    private void OnPeek(InputAction.CallbackContext context)
    {
        HandleHoldAction(context, _peekHoldState, EAction.Peek);
    }

    private void OnUtility(InputAction.CallbackContext context)
    {
        HandleHoldAction(context, _utilityHoldState, EAction.Utility);
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Cancel);
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        RequestAction(EAction.Pause);
    }

    private void OnHelp(InputAction.CallbackContext context)
    {
        _deviceManager.UpdateLastKnownDevice(context.control.device);
        HandleHoldAction(context, _helpHoldState, EAction.Help);
    }

    private void RequestDirectionalAction(EDirectionalAction actionType, Vector2 input, bool isMouse)
    {
        DirectionalActionEvent?.Invoke(actionType, input, isMouse);
#if LOG_ACTIONS
         Debug.Log($"{actionType} requested with input: {input}, isMouse: {isMouse}");
#endif
    }

    private void HandleHoldAction(InputAction.CallbackContext context, HoldState holdState, EAction actionType)
    {
        if (context.started)
        {
            holdState.OnPressInput(out bool canProcessInput);
            if (!canProcessInput) return;

            PreHoldActionEvent?.Invoke(actionType);
        }
        else if (context.canceled && !_mapper.SupressCancelOnScopeChange(actionType))
        {
            holdState.OnReleaseInput(out bool wasHolding, out bool canProcessInput);
            if (!canProcessInput) return;

            if (wasHolding)
            {
                RequestHoldAction(actionType, false);
            }
            else
            {
                RequestAction(actionType);
            }
        }
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
        if (_isBlocked) return;

        ActionEvent?.Invoke(actionType);
#if LOG_ACTIONS
    Debug.Log($"{actionType} requested");
#endif
    }
}

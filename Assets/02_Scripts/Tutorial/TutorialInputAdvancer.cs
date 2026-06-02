using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class TutorialInputAdvancer : MonoBehaviour
{
    [SerializeField] private float _inputDelay = 1f;
    [SerializeField] private UnityEvent _onHide;
    [SerializeField] private UnityEvent[] _onNext;

    private bool _active;
    private bool _waitingForRelease;
    private float _activationTime;
    private int _nextIndex;

    public void Activate()
    {
        _active = true;
        _activationTime = Time.time;
        _waitingForRelease = AnyInputHeld();
    }

    public void Deactivate()
    {
        _active = false;
    }

    private void Update()
    {
        if (!_active) return;

        if (_waitingForRelease)
        {
            _waitingForRelease = AnyInputHeld();
            return;
        }

        if (Time.time - _activationTime < _inputDelay) return;
        if (AnyInputDown())
        {
            Advance();
        }
    }

    public void Advance()
    {
        if (!_active) return;
        _waitingForRelease = true;

        if (_nextIndex < _onNext.Length)
        {
            UnityEvent onNext = _onNext[_nextIndex];
            _nextIndex++;
            onNext?.Invoke();
        }
        else
        {
            _onHide?.Invoke();
        }
    }

    private bool AnyInputDown()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame)
                return true;
        }
        return false;
    }

    private bool AnyInputHeld()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.buttonSouth.isPressed ||
                gamepad.buttonEast.isPressed ||
                gamepad.startButton.isPressed)
                return true;
        }
        return false;
    }
}

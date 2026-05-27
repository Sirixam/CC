using UnityEngine;

[DefaultExecutionOrder(-10)]
public class HandAnimator : MonoBehaviour
{
    public enum State { Hidden, Writing, Validating, Crafting, Slapping }

    [SerializeField] private EDominantHand _dominantHand;
    [SerializeField] private HandView _leftHand;
    [SerializeField] private HandView _rightHand;
    [SerializeField] private float _handMoveSpeed = 3f;

    [Header("Slap")]
    [SerializeField] private HandSlapMotion _leftSlapMotion;
    [SerializeField] private HandSlapMotion _rightSlapMotion;
    [SerializeField] private HandSlapMotion _frontSlapMotion;
    [SerializeField, Range(0f, 90f)] private float _slapFrontHalfAngle = 45f;

    public State CurrentState { get; private set; }

    private bool _isLefty;
    private bool IsOneShot => CurrentState == State.Slapping;
    private State _deferredState;
    private HandSlapMotion _activeSlapMotion;

    private void Awake()
    {
        _isLefty = _dominantHand == EDominantHand.Left ||
                   (_dominantHand == EDominantHand.Random && UnityEngine.Random.value < 0.5f);

        _leftHand.SetIsLefty(true);
        _rightHand.SetIsLefty(false);
        SetHidden();
    }

    private void Update()
    {
        if (CurrentState != State.Validating) return;
        _leftHand.MoveTowardTarget(_handMoveSpeed);
        _rightHand.MoveTowardTarget(_handMoveSpeed);
    }

    // --- Public state setters ---

    public void SetHidden()
    {
        if (IsOneShot) { _deferredState = State.Hidden; return; }
        ApplyHidden();
    }

    public void SetWriting()
    {
        if (IsOneShot) { _deferredState = State.Writing; return; }
        ApplyWriting();
    }

    public void SetValidating()
    {
        if (IsOneShot) { _deferredState = State.Validating; return; }
        ApplyValidating();
    }

    public void SetCrafting()
    {
        if (IsOneShot) { _deferredState = State.Crafting; return; }
        ApplyCrafting();
    }

    // --- One-shot ---

    [Button("Play Slap")]
    public void PlaySlap() => PlaySlap(transform.position + transform.forward);

    public void PlaySlap(Vector3 targetWorldPos)
    {
        if (!IsOneShot)
            _deferredState = CurrentState;
        else
            _activeSlapMotion?.Cancel();

        CurrentState = State.Slapping;

        (HandSlapMotion motion, HandView hand) = SelectSlap(targetWorldPos);
        _activeSlapMotion = motion;

        hand.WritingLoopController.enabled = false;
        hand.CrumplingController.enabled = false;
        hand.PinchController.Release();
        hand.HidePencil();
        hand.Show();

        motion.Play(() =>
        {
            _activeSlapMotion = null;
            ApplyState(_deferredState);
        });
    }

    // --- Private apply methods (bypass one-shot guard) ---

    private void ApplyHidden()
    {
        CurrentState = State.Hidden;
        _leftHand.WritingLoopController.enabled = false;
        _rightHand.WritingLoopController.enabled = false;
        _leftHand.CrumplingController.enabled = false;
        _rightHand.CrumplingController.enabled = false;
        _leftHand.PinchController.Release();
        _rightHand.PinchController.Release();
        _leftHand.HidePencil();
        _rightHand.HidePencil();
        _leftHand.Hide();
        _rightHand.Hide();
    }

    private void ApplyWriting()
    {
        CurrentState = State.Writing;

        var dominant = _isLefty ? _leftHand : _rightHand;
        var other = _isLefty ? _rightHand : _leftHand;

        other.WritingLoopController.enabled = false;
        other.CrumplingController.enabled = false;
        other.PinchController.Release();
        other.HidePencil();
        other.Hide();

        dominant.Show();
        dominant.ShowPencil();
        dominant.PinchController.Pinch();
        dominant.WritingLoopController.enabled = true;
    }

    private void ApplyValidating()
    {
        CurrentState = State.Validating;
        _leftHand.WritingLoopController.enabled = false;
        _rightHand.WritingLoopController.enabled = false;
        _leftHand.CrumplingController.enabled = false;
        _rightHand.CrumplingController.enabled = false;
        _leftHand.HidePencil();
        _rightHand.HidePencil();
        _leftHand.PinchController.Pinch();
        _rightHand.PinchController.Pinch();
        _leftHand.Show();
        _rightHand.Show();
    }

    private void ApplyCrafting()
    {
        CurrentState = State.Crafting;
        _leftHand.WritingLoopController.enabled = false;
        _rightHand.WritingLoopController.enabled = false;
        _leftHand.HidePencil();
        _rightHand.HidePencil();
        _leftHand.PinchController.Pinch();
        _rightHand.PinchController.Pinch();
        _leftHand.CrumplingController.enabled = true;
        _rightHand.CrumplingController.enabled = true;
        _leftHand.Show();
        _rightHand.Show();
    }

    private void ApplyState(State state)
    {
        switch (state)
        {
            case State.Hidden:     ApplyHidden();     break;
            case State.Writing:    ApplyWriting();    break;
            case State.Validating: ApplyValidating(); break;
            case State.Crafting:   ApplyCrafting();   break;
        }
    }

    private (HandSlapMotion, HandView) SelectSlap(Vector3 targetWorldPos)
    {
        Vector3 dir = targetWorldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return (_frontSlapMotion, _isLefty ? _leftHand : _rightHand);
        dir.Normalize();

        Vector3 forward = transform.forward; forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();

        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(dir, forward), -1f, 1f)) * Mathf.Rad2Deg;
        if (angle <= _slapFrontHalfAngle)
            return (_frontSlapMotion, _isLefty ? _leftHand : _rightHand);

        Vector3 right = transform.right; right.y = 0f;
        return Vector3.Dot(dir, right) >= 0f
            ? (_rightSlapMotion, _rightHand)
            : (_leftSlapMotion, _leftHand);
    }
}

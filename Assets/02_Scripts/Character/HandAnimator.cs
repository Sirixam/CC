using UnityEngine;

[DefaultExecutionOrder(-10)]
public class HandAnimator : MonoBehaviour
{
    public enum State { Hidden, Writing, Validating }

    [SerializeField] private EDominantHand _dominantHand;
    [SerializeField] private HandView _leftHand;
    [SerializeField] private HandView _rightHand;
    [SerializeField] private float _handMoveSpeed = 3f;

    public State CurrentState { get; private set; }

    private bool _isLefty;

    private void Awake()
    {
        _isLefty = _dominantHand == EDominantHand.Left ||
                   (_dominantHand == EDominantHand.Random && UnityEngine.Random.value < 0.5f);
        SetHidden();
    }

    private void Update()
    {
        if (CurrentState != State.Validating) return;
        _leftHand.MoveTowardTarget(_handMoveSpeed);
        _rightHand.MoveTowardTarget(_handMoveSpeed);
    }

    public void SetHidden()
    {
        CurrentState = State.Hidden;
        _leftHand.WritingLoopController.enabled = false;
        _rightHand.WritingLoopController.enabled = false;
        _leftHand.PinchController.Release();
        _rightHand.PinchController.Release();
        _leftHand.HidePencil();
        _rightHand.HidePencil();
        _leftHand.Hide();
        _rightHand.Hide();
    }

    public void SetWriting()
    {
        CurrentState = State.Writing;
        _leftHand.PinchController.Release();
        _rightHand.PinchController.Release();

        var dominant = _isLefty ? _leftHand : _rightHand;
        var other    = _isLefty ? _rightHand : _leftHand;

        other.WritingLoopController.enabled = false;
        other.HidePencil();
        other.Hide();

        dominant.Show();
        dominant.ShowPencil();
        dominant.WritingLoopController.enabled = true;
    }

    public void SetValidating()
    {
        CurrentState = State.Validating;
        _leftHand.WritingLoopController.enabled = false;
        _rightHand.WritingLoopController.enabled = false;
        _leftHand.HidePencil();
        _rightHand.HidePencil();
        _leftHand.PinchController.Pinch();
        _rightHand.PinchController.Pinch();
        _leftHand.Show();
        _rightHand.Show();
    }
}

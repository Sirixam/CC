using UnityEngine;

[DefaultExecutionOrder(-10)]
public class HandAnimator : MonoBehaviour
{
    public enum State { Hidden, Writing, Validating, Crafting }

    [SerializeField] private EDominantHand _dominantHand;
    [SerializeField] private HandView _leftHand;
    [SerializeField] private HandView _rightHand;
    [SerializeField] private HandSlapController _slapController;
    [SerializeField] private float _handMoveSpeed = 3f;

    public State CurrentState { get; private set; }

    private bool _isLefty;

    private void Awake()
    {
        _isLefty = _dominantHand == EDominantHand.Left ||
                   (_dominantHand == EDominantHand.Random && UnityEngine.Random.value < 0.5f);

        _leftHand.SetIsLefty(true);
        _rightHand.SetIsLefty(false);
        _slapController?.Initialize(_isLefty, _leftHand, _rightHand);
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
        _leftHand.CrumplingController.enabled = false;
        _rightHand.CrumplingController.enabled = false;
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

    public void SetValidating()
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

    [Button("Play Slap")]
    public void PlaySlap() => PlaySlap(transform.position + transform.forward);

    public void PlaySlap(Vector3 targetWorldPos)
    {
        if (_slapController == null)
        {
            Debug.LogError("Slap controller is not assigned: " + name, gameObject);
            return;
        }

        HandView hand = _slapController.Play(transform, targetWorldPos, ReapplyCurrentState);
        hand.WritingLoopController.enabled = false;
        hand.CrumplingController.enabled = false;
        hand.PinchController.Release();
        hand.HidePencil();
        hand.Show();
    }

    private void ReapplyCurrentState()
    {
        switch (CurrentState)
        {
            case State.Hidden: SetHidden(); break;
            case State.Writing: SetWriting(); break;
            case State.Validating: SetValidating(); break;
            case State.Crafting: SetCrafting(); break;
        }
    }

    public void SetCrafting()
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
}

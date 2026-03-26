using UnityEngine;

public class StudentView : MonoBehaviour
{
    [SerializeField] private EDominantHand _dominantHand;
    [SerializeField] private HandView _leftHandView;
    [SerializeField] private HandView _rightHandView;
    [SerializeField] private float _handMoveSpeed = 3f;

    private bool _isLefty;
    private bool _isValidating;

    private void Awake()
    {
        _isLefty = _dominantHand == EDominantHand.Left || (_dominantHand == EDominantHand.Random && UnityEngine.Random.value < 0.5f);
    }

    private void Update()
    {
        if (!_isValidating) return;
        _leftHandView.MoveTowardTarget(_handMoveSpeed);
        _rightHandView.MoveTowardTarget(_handMoveSpeed);
    }

    public void StartThinking(TestPageView testPageView)
    {
        _isValidating = false;

        _leftHandView.PinchController.Release();
        _rightHandView.PinchController.Release();

        _leftHandView.HidePencil();
        _rightHandView.HidePencil();

        _leftHandView.Hide();
        _rightHandView.Hide();

        testPageView?.Lower();
    }

    public void StartAnswering()
    {
        _isValidating = false;

        if (_isLefty)
        {
            _leftHandView.Show();
            _leftHandView.ShowPencil();
            _leftHandView.WritingLoopController.enabled = true;
        }
        else
        {
            _rightHandView.Show();
            _rightHandView.ShowPencil();
            _rightHandView.WritingLoopController.enabled = true;
        }
    }

    public void StartValidating(TestPageView testPageView)
    {
        _isValidating = true;

        _leftHandView.WritingLoopController.enabled = false;
        _rightHandView.WritingLoopController.enabled = false;

        _leftHandView.HidePencil();
        _rightHandView.HidePencil();

        _leftHandView.PinchController.Pinch();
        _rightHandView.PinchController.Pinch();

        _leftHandView.Show();
        _rightHandView.Show();

        testPageView?.Lift();
    }
}

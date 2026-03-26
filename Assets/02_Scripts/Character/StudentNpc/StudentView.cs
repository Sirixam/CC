using AKGaming.Game;
using System;
using UnityEngine;

public class StudentView : MonoBehaviour
{
    [Serializable]
    public class HandView
    {
        [SerializeField] private GameObject _root;

        public HandWritingLoopController WritingLoopController;
        public HandPinchController PinchController;
        public Transform ValidatingTarget;
        public GameObject Pencil;

        public void Initialize()
        {
            _root.SetActive(false);
            if (Pencil != null) Pencil.SetActive(false);
        }

        public void ShowPencil() { if (Pencil != null) Pencil.SetActive(true); }
        public void HidePencil() { if (Pencil != null) Pencil.SetActive(false); }

        public void Show()
        {
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        public void MoveTowardTarget(float speed)
        {
            if (ValidatingTarget == null) return;
            _root.transform.position = Vector3.MoveTowards(
                _root.transform.position,
                ValidatingTarget.position,
                speed * Time.deltaTime);
        }
    }

    public enum EDominantHand
    {
        Undefined,
        Left,
        Right,
        Random,
    }

    [SerializeField] private EDominantHand _dominantHand;
    [SerializeField] private HandView _leftHandView;
    [SerializeField] private HandView _rightHandView;
    [SerializeField] private float _handMoveSpeed = 3f;

    private bool _isLefty;
    private bool _isValidating;

    private void Awake()
    {
        _leftHandView.Initialize();
        _rightHandView.Initialize();

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

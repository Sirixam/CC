using UnityEngine;

public class HandCrumplingController : MonoBehaviour
{
    [SerializeField] private Transform _handRoot;
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _radius = 0.015f;

    private Vector3 _initialPos;
    private float _angle;
    private bool _reverseDirection;

    private void Awake()
    {
        _initialPos = _handRoot.localPosition;
    }

    private void OnEnable()
    {
        _initialPos = _handRoot.localPosition;
        _angle = 0f;
    }

    private void OnDisable()
    {
        if (_handRoot != null)
            _handRoot.localPosition = _initialPos;
    }

    public void SetReverseDirection(bool value)
    {
        _reverseDirection = value;
    }

    private void Update()
    {
        _angle += _speed * (_reverseDirection ? -1f : 1f) * Time.deltaTime;
        _handRoot.localPosition = _initialPos + new Vector3(
            Mathf.Cos(_angle) * _radius,
            Mathf.Sin(_angle) * _radius,
            0f
        );
    }
}

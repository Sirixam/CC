using System;
using UnityEngine;

[Serializable]
public class PlayerPhysics
{
    [SerializeField] private Rigidbody _rigidbody;
    [Header("Configurations")]
    [SerializeField] private float _moveSpeed = 5f; // Meters per second
    [SerializeField] private float _dashSpeed = 10f; // Meters per second
    [SerializeField] private float _dashDuration = 0.4f; // Seconds

    // Movement
    private Vector3 _moveDirection;
    // Dash
    private bool _isDashing;
    private float _dashTimer;
    private Vector3 _dashDirection;

    public void Initialize()
    {
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping over
    }

    public void SetMoveDirection(Vector3 value)
    {
        _moveDirection = value;
    }

    public void StartDashing(Vector3 direction)
    {
        _isDashing = true;
        _dashDirection = direction;
        _dashTimer = _dashDuration;
    }

    public void OnFixedUpdate(float deltaTime)
    {
        if (_isDashing)
        {
            _rigidbody.velocity = _dashDirection * _dashSpeed;

            _dashTimer -= deltaTime;
            if (_dashTimer < 0)
            {
                _isDashing = false;
            }
        }
        else
        {
            _rigidbody.velocity = _moveDirection * _moveSpeed;
        }
    }
}

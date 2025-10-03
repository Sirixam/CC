using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerPhysics
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collider;
    [Header("Configurations")]
    [SerializeField] private float _moveSpeed = 5f; // Meters per second
    [SerializeField] private float _dashSpeed = 15f; // Meters per second
    [SerializeField] private float _dashDuration = 0.3f; // Seconds
    [SerializeField] private float _frontalCollisionAngle = 30f;
    [SerializeField] private float _stopDistance = 0.05f;

    // Movement
    private Vector3 _moveDirection;
    private Transform _targetPoint;
    // Dash
    public bool IsDashing { get; private set; }
    private float _dashTimer;
    private Vector3 _dashDirection;
    // Collisions
    private List<Vector3> _collisionNormals = new();
    public Collider Collider => _collider;

    // General
    public Vector3 Direction => IsDashing ? _dashDirection : _moveDirection;

    public event Action OnArriveEvent;

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
        IsDashing = true;
        _dashDirection = direction;
        _dashTimer = _dashDuration;
    }

    public bool TryStopDashing()
    {
        if (!IsDashing) return false;

        IsDashing = false;
        _dashTimer = 0;
        return true;
    }

    public void OnFixedUpdate(float deltaTime, bool canMove, out bool stoppedDashing)
    {
        stoppedDashing = false;
        if (!canMove)
        {
            _collisionNormals.Clear();
            _rigidbody.velocity = Vector3.zero;
            return;
        }

        if (_targetPoint != null)
        {
            if (IsDashing)
            {
                IsDashing = false;
                stoppedDashing = true;
            }

            MoveTowardsTarget(_targetPoint, out bool hasArrived); // [AKP] This is ignoring sliding for now
            if (hasArrived)
            {
                OnArriveEvent?.Invoke();
            }
            return;
        }

        Vector3 velocity;
        if (IsDashing)
        {
            velocity = _dashDirection * _dashSpeed;

            _dashTimer -= deltaTime;
            if (_dashTimer < 0)
            {
                IsDashing = false;
                stoppedDashing = true;
            }
        }
        else
        {
            velocity = _moveDirection * _moveSpeed;
        }

        // Handle sliding
        foreach (var normal in _collisionNormals)
        {
            float alignment = Vector3.Dot(velocity, -normal);
            if (alignment <= 0) continue;

            velocity = Vector3.ProjectOnPlane(velocity, normal);
        }
        _collisionNormals.Clear();

        // Apply velocity
        _rigidbody.velocity = velocity;
    }

    private void MoveTowardsTarget(Transform target, out bool hasArrived)
    {
        if (target == null)
        {
            hasArrived = false;
            return;
        }

        Vector3 toTarget = target.position - _rigidbody.position;
        float distance = toTarget.magnitude;
        Vector3 direction = toTarget.normalized;

        Vector3 newPosition = _rigidbody.position + direction * _moveSpeed * Time.fixedDeltaTime;
        hasArrived = (newPosition - target.position).sqrMagnitude >= distance * distance;
        if (hasArrived)
        {
            newPosition = target.position; // Prevent overshooting
        }
        _rigidbody.MovePosition(newPosition);
    }

    public void SetTargetPoint(Transform point)
    {
        _targetPoint = point;
    }

    public void AddCollisionNormal(Vector3 value)
    {
        _collisionNormals.Add(value);
    }

    public void ClearCollisionNormals()
    {
        _collisionNormals.Clear();
    }

    public bool IsFrontalCollision(Vector3 collisionNormal)
    {
        float alignment = Vector3.Dot(Direction, -collisionNormal);
        alignment = Mathf.Clamp(alignment, -1f, 1f); // Clamp just in case of float precision errors

        float angle = Mathf.Acos(alignment) * Mathf.Rad2Deg;
        return angle <= _frontalCollisionAngle;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class PlayerPhysics
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider[] _colliders;
    [Header("Configurations")]
    [SerializeField] private float _moveSpeed = 5f; // Meters per second
    [SerializeField] private float _dashSpeed = 15f; // Meters per second
    [SerializeField] private float _dashDuration = 0.3f; // Seconds
    [SerializeField] private float _frontalCollisionAngle = 30f;

    // Movement
    private Vector3 _inputDirection;
    private Vector3 _moveDirection;
    private Transform _targetPoint;
    private float _moveSpeedMultiplier = 1f;
    public Vector3 Position => _rigidbody.position;

    // Navmesh pathing
    private Vector3[] _pathCorners;
    private int _currentCornerIndex;
    private bool _isFollowingPath;
    public bool IsFollowingPath => _isFollowingPath;


    // Dash
    public bool IsDashing { get; private set; }
    private float _dashTimer;
    private Vector3 _dashDirection;
    // Collisions
    private List<Vector3> _collisionNormals = new();
    public Collider[] Colliders => _colliders;

    // General
    public Vector3 Direction => IsDashing ? _dashDirection : _moveDirection;

    public event Action OnArriveEvent;

    public void Initialize()
    {
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping over
    }

    public void SetInputDirection(Vector3 value)
    {
        _inputDirection = value;
    }

    public void SetMoveDirection(Vector3 value)
    {
        _moveDirection = value;
    }

    public void StartDashing(Vector3 forward)
    {
        IsDashing = true;
        _dashDirection = _inputDirection != Vector3.zero ? _inputDirection : forward;
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
        if (_isFollowingPath && _pathCorners != null)
        {
            Vector3 target = _pathCorners[_currentCornerIndex];
            Vector3 toTarget = target - _rigidbody.position;
            toTarget.y = 0;
            float distance = toTarget.magnitude;

            if (distance < 0.1f)
            {
                _currentCornerIndex++;
                if (_currentCornerIndex >= _pathCorners.Length)
                {
                    _isFollowingPath = false;
                    _pathCorners = null;
                    OnArriveEvent?.Invoke();
                    _rigidbody.velocity = Vector3.zero;
                    return;
                }
            }
            else
            {
                Vector3 direction = toTarget.normalized;
                _rigidbody.MovePosition(_rigidbody.position + direction * _moveSpeed * deltaTime);
            }

            _collisionNormals.Clear();
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
            velocity = _moveDirection * _moveSpeed * _moveSpeedMultiplier;
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

    public void TeleportToPoint(Transform point)
    {
        if (point == null) return;
        _rigidbody.position = point.position;
        _rigidbody.velocity = Vector3.zero;
        _targetPoint = null;
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

    public void SetMoveSpeedMultiplier(float value)
    {
        _moveSpeedMultiplier = value;
    }

    public void ApplyImpulse(Vector3 force)
    {
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }
    public void ForceStopDash()
    {
        if (IsDashing)
        {
            IsDashing = false;
            _dashTimer = 0;
            _rigidbody.velocity = Vector3.zero;
        }
    }

    public bool StartFollowingNavMeshPath(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(_rigidbody.position, destination, NavMesh.AllAreas, path))
        {
            _pathCorners = path.corners;
            _currentCornerIndex = 1; // skip first corner (current position)
            _isFollowingPath = true;
            return true;
        }
        return false;
    }

    public void StopFollowingPath()
    {
        _isFollowingPath = false;
        _pathCorners = null;
        _currentCornerIndex = 0;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class PlayerPhysics
{
    public enum EForce
    {
        None,
        Dash,
        External
    }

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
    public Vector3 PathDirection { get; private set; }

    // Force
    public EForce ForceType { get; private set; }
    private float _forceTimer;
    private Vector3 _force;
    // Collisions
    private List<Vector3> _collisionNormals = new();
    public Collider[] Colliders => _colliders;

    // General
    public Vector3 Direction => ForceType == EForce.None ? _moveDirection : _force;
    public bool IsDashing => ForceType == EForce.Dash;

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
        ForceType = EForce.Dash;
        Vector3 direction = _inputDirection != Vector3.zero ? _inputDirection : forward;
        _force = direction * _dashSpeed;
        _forceTimer = _dashDuration;
    }

    public void StartExternalForce(Vector3 force)
    {
        ForceType = EForce.External;
        _force = force;
        _forceTimer = _dashDuration;
    }

    public bool TryStopForce()
    {
        if (ForceType == EForce.None) return false;

        ForceType = EForce.None;
        _forceTimer = 0;
        return true;
    }

    public void OnFixedUpdate(float deltaTime, bool canMove, out bool stoppedForce)
    {
        stoppedForce = false;
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

            if (distance < 0.2f)
            {
                _currentCornerIndex++;
                if (_currentCornerIndex >= _pathCorners.Length)
                {
                    _isFollowingPath = false;
                    _pathCorners = null;
                    _rigidbody.velocity = Vector3.zero;
                    OnArriveEvent?.Invoke();
                    return;
                }
            }
            else
            {
                Vector3 direction = toTarget.normalized;
                PathDirection = direction;
                _rigidbody.velocity = direction * _moveSpeed;
            }

            _collisionNormals.Clear();
            return;
        }

        if (_targetPoint != null)
        {
            if (ForceType != EForce.None)
            {
                ForceType = EForce.None;
                stoppedForce = true;
            }

            MoveTowardsTarget(_targetPoint, out bool hasArrived); // [AKP] This is ignoring sliding for now
            if (hasArrived)
            {
                OnArriveEvent?.Invoke();
            }
            return;
        }

        Vector3 velocity;
        if (ForceType == EForce.None)
        {
            velocity = _moveDirection * _moveSpeed * _moveSpeedMultiplier;
        }
        else
        {
            velocity = _force;

            _forceTimer -= deltaTime;
            if (_forceTimer < 0)
            {
                ForceType = EForce.None;
                stoppedForce = true;
            }
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

    public void ForceStopForce()
    {
        if (ForceType != EForce.None)
        {
            ForceType = EForce.None;
            _forceTimer = 0;
            _rigidbody.velocity = Vector3.zero;
        }
    }

    public bool StartFollowingNavMeshPath(Vector3 destination, Vector3? avoidPosition = null, float avoidRadius = 2f)
    {
        if (!NavMesh.SamplePosition(_rigidbody.position, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
            return false;

        if (!NavMesh.SamplePosition(destination, out NavMeshHit endHit, 2f, NavMesh.AllAreas))
            return false;

        if (avoidPosition.HasValue)
        {
            Vector3 start = startHit.position;
            Vector3 end = endHit.position;
            Vector3 avoid = avoidPosition.Value;

            // Check if direct path passes near the avoidance point
            Vector3 toEnd = (end - start).normalized;
            Vector3 toAvoid = avoid - start;
            float projection = Vector3.Dot(toAvoid, toEnd);
            Vector3 closestPoint = start + toEnd * Mathf.Clamp(projection, 0, Vector3.Distance(start, end));
            float distToPath = Vector3.Distance(closestPoint, avoid);

            if (distToPath < avoidRadius)
            {
                // Calculate a waypoint to the side
                Vector3 perpendicular = Vector3.Cross(toEnd, Vector3.up).normalized;
                Vector3 waypointA = avoid + perpendicular * avoidRadius * 2f;
                Vector3 waypointB = avoid - perpendicular * avoidRadius * 2f;
                Vector3 waypoint = Vector3.Distance(start, waypointA) < Vector3.Distance(start, waypointB)
                    ? waypointA : waypointB;

                if (NavMesh.SamplePosition(waypoint, out NavMeshHit waypointHit, 3f, NavMesh.AllAreas))
                {
                    // Use NavMesh pathfinding for each segment
                    NavMeshPath pathA = new NavMeshPath();
                    NavMeshPath pathB = new NavMeshPath();

                    if (NavMesh.CalculatePath(startHit.position, waypointHit.position, NavMesh.AllAreas, pathA) &&
                        NavMesh.CalculatePath(waypointHit.position, endHit.position, NavMesh.AllAreas, pathB))
                    {
                        // Merge both paths
                        var allCorners = new List<Vector3>(pathA.corners);
                        // Skip first corner of pathB (it's the same as last corner of pathA)
                        for (int i = 1; i < pathB.corners.Length; i++)
                            allCorners.Add(pathB.corners[i]);

                        _pathCorners = allCorners.ToArray();
                        _currentCornerIndex = 1;
                        _isFollowingPath = true;

                        for (int i = 0; i < _pathCorners.Length - 1; i++)
                            Debug.DrawLine(_pathCorners[i], _pathCorners[i + 1], Color.green, 5f);

                        return true;
                    }
                }
            }
        }

        // Default: direct navmesh path
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                _pathCorners = path.corners;
                _currentCornerIndex = 1;
                _isFollowingPath = true;

                for (int i = 0; i < _pathCorners.Length - 1; i++)
                    Debug.DrawLine(_pathCorners[i], _pathCorners[i + 1], Color.green, 5f);

                return true;
            }
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

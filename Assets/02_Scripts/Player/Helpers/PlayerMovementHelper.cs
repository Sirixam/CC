using System;
using UnityEngine;

public class PlayerMovementHelper
{
    [Serializable]
    public class Data
    {
        public float LookSpeed = 1080f; // Degrees per second
        public float DashCooldown = 0.2f; // Seconds
        [Tag] public string[] HardCollisionTags;
    }

    private readonly PlayerView _view;
    private readonly PlayerPhysics _physics;
    private readonly Data _data;

    private float _dashCooldownTimer;
    private Vector3 _lookDirection;
    private Transform _lookAtPoint;

    public Vector3 LookDirection => _lookDirection;

    public PlayerMovementHelper(PlayerView view, PlayerPhysics physics, Data data)
    {
        _view = view;
        _physics = physics;
        _data = data;
    }

    public void Initialize(Vector3 startForward)
    {
        _lookDirection = startForward;
    }

    public void SetLookAt(Transform lookAtPoint) => _lookAtPoint = lookAtPoint;

    public void ClearLookAt() => _lookAtPoint = null;

    public void SetLookInput(Vector2 input)
    {
        if (input != Vector2.zero)
        {
            _lookDirection = new Vector3(input.x, 0, input.y);
        }
    }

    public void RequestDash()
    {
        if (_dashCooldownTimer <= 0)
        {
            _view.OnStartDash();
            _physics.StartDashing(_lookDirection);
            _dashCooldownTimer = _data.DashCooldown;
        }
    }

    public void UpdateRotation(Transform transform)
    {
        if (_lookAtPoint != null)
        {
            Vector3 lookPosition = _lookAtPoint.position;
            lookPosition.y = transform.position.y;
            _lookDirection = (lookPosition - transform.position).normalized;
        }

        if (_lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _data.LookSpeed * Time.deltaTime);
        }
    }

    public void UpdateCooldown() => _dashCooldownTimer -= Time.deltaTime;

    public void OnCollisionStay(Collision collision, Action<bool> OnStopDash)
    {
        foreach (var contact in collision.contacts)
        {
            if (_physics.IsFrontalCollision(contact.normal))
            {
                _physics.ClearCollisionNormals();
                if (_physics.TryStopDashing())
                {
                    _view.OnStopDash();
                    bool isSoftStun = !HasAnyTag(collision.transform, _data.HardCollisionTags);
                    OnStopDash.Invoke(isSoftStun);
                }
                return;
            }
            _physics.AddCollisionNormal(contact.normal);
        }
    }

    private bool HasAnyTag(Transform target, string[] tags)
    {
        foreach (var tag in tags)
        {
            if (target.CompareTag(tag)) return true;
        }
        return false;
    }
}

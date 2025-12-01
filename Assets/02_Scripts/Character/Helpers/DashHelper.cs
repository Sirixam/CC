using System;
using UnityEngine;

public class DashHelper
{
    [Serializable]
    public class Data
    {
        public float DashCooldown = 0.2f; // Seconds
        [Tag] public string[] HardCollisionTags;
    }

    private readonly PlayerView _view;
    private readonly PlayerPhysics _physics;
    private readonly LookHelper _lookHelper;
    private readonly Data _data;

    private float _dashCooldownTimer;

    public DashHelper(PlayerView view, PlayerPhysics physics, LookHelper lookHelper, Data data)
    {
        _view = view;
        _physics = physics;
        _lookHelper = lookHelper;
        _data = data;
    }

    public void RequestDash()
    {
        if (_dashCooldownTimer <= 0)
        {
            _view.OnStartDash();
            _physics.StartDashing(_view.transform.forward);
            _dashCooldownTimer = _data.DashCooldown;
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

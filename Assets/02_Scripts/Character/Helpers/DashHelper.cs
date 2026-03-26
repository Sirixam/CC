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

    private readonly Data _data;
    private readonly PlayerView _view;
    private readonly PlayerPhysics _physics;
    private readonly LookHelper _lookHelper;
    private readonly PlayerAudioHelper _audioHelper;
    private float _dashCooldownTimer;

    public DashHelper(Data data, PlayerView view, PlayerPhysics physics, LookHelper lookHelper, PlayerAudioHelper audioHelper)
    {
        _data = data;
        _view = view;
        _physics = physics;
        _lookHelper = lookHelper;
        _audioHelper = audioHelper;
    }

    public bool CanDash()
    {
        return _dashCooldownTimer <= 0;
    }

    public void StartDash()
    {
        _view.OnStartForce();
        _physics.StartDashing(_view.transform.forward);
        _dashCooldownTimer = _data.DashCooldown;
        _audioHelper.OnStartDash();
    }

    public void UpdateCooldown() => _dashCooldownTimer -= Time.deltaTime;

    public void OnCollisionStay(Collision collision, Action<bool> onStopDash)
    {
        foreach (var contact in collision.contacts)
        {
            if (_physics.IsFrontalCollision(contact.normal))
            {
                _physics.ClearCollisionNormals();
                if (_physics.TryStopForce())
                {
                    _view.OnStopForce();
                    bool isSoftStun = !HasAnyTag(collision.transform, _data.HardCollisionTags);
                    onStopDash?.Invoke(isSoftStun);
                    _audioHelper.OnStun();
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

using System;
using UnityEngine;

public class DynamicLobThrowHelper
{
    [Serializable]
    public class Data
    {
        public float MinRange = 3f;
        public float MaxRange = 10f;
        public float MaxHeight = 4f;
        public float CycleSpeed = 2f;
        [Tooltip("1 = normal, 2 = twice as fast, same height")]
        public float SpeedMultiplier = 1f;
        [Tooltip("If true, range ping-pongs. If false, resets to min after reaching max")]
        public float SpinForce = 10f;
        public bool PingPong = true;
        
    }

    private Data _data;
    private IThrowActor _actor;
    private InteractionHelper _interactionHelper;
    private int _flyingLayer;

    private bool _isCharging;
    private float _chargeTimer;
    private float _currentRange;

    public Data GetData() => _data;
    public float CurrentRange => _currentRange;
    public bool IsCharging => _isCharging;

    public DynamicLobThrowHelper(Data data, IThrowActor actor, InteractionHelper interactionHelper, int flyingLayer)
    {
        _data = data;
        _actor = actor;
        _interactionHelper = interactionHelper;
        _flyingLayer = flyingLayer;
    }

    public bool CanShowPreview()
    {
        return _interactionHelper.TryGetPickedUpInteraction(out InteractionController interaction)
            && interaction.GetComponent<PaperBallController>()?.IsDynamicLobShot == true;
    }

    public void StartCharging()
    {
        _isCharging = true;
        _chargeTimer = 0f;
        _currentRange = _data.MinRange;
    }

    public void StopCharging()
    {
        _isCharging = false;
    }

    public void UpdateCharging()
    {
        if (!_isCharging) return;

        _chargeTimer += Time.deltaTime;
        float t = _chargeTimer / _data.CycleSpeed;

        if (_data.PingPong)
        {
            // Ping pong: 0→1→0→1...
            t = Mathf.PingPong(t, 1f);
        }
        else
        {
            // Clamp: 0→1 then stay at 1
            t = Mathf.Repeat(t, 1f);
        }

        _currentRange = Mathf.Lerp(_data.MinRange, _data.MaxRange, t);
    }

    public Vector3 CalculateLobVelocity(Vector3 horizontalDirection)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float effectiveG = g * _data.SpeedMultiplier * _data.SpeedMultiplier;
        float vy = Mathf.Sqrt(2f * effectiveG * _data.MaxHeight);
        float totalTime = 2f * vy / effectiveG;
        float vHorizontal = _currentRange / totalTime;

        return horizontalDirection * vHorizontal + Vector3.up * vy;
    }

    public float GetFlightDuration()
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float effectiveG = g * _data.SpeedMultiplier * _data.SpeedMultiplier;
        float vy = Mathf.Sqrt(2f * effectiveG * _data.MaxHeight);
        return 2f * vy / effectiveG;
    }

    public bool TryTriggerThrow()
    {
        if (!_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
            return false;

        var paperBall = stoppedInteraction.GetComponent<PaperBallController>();
        if (paperBall == null || !paperBall.IsDynamicLobShot) return false;

        _interactionHelper.TryStopInteraction(stoppedInteraction);
        if (stoppedInteraction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
            interactionOwner.OnThrowed();

        _actor.OnThrow(stoppedInteraction.transform);

        Vector3 throwDirection = _actor.LookDirection;
        throwDirection.y = 0;
        throwDirection.Normalize();

        Vector3 velocity = CalculateLobVelocity(throwDirection);
        stoppedInteraction.Rigidbody.AddForce(velocity, ForceMode.VelocityChange);

        // Add spin
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0f, 1f)
        ).normalized * _data.SpinForce;
        stoppedInteraction.Rigidbody.AddTorque(randomTorque.normalized * _data.SpinForce, ForceMode.VelocityChange);

        // Add extra gravity component
        float extraGravityScale = _data.SpeedMultiplier * _data.SpeedMultiplier - 1f;
        if (extraGravityScale > 0f)
        {
            var extraGravity = stoppedInteraction.gameObject.AddComponent<ExtraGravity>();
            extraGravity.Scale = extraGravityScale;
        }

        CollisionComponent collisionComponent = stoppedInteraction.GetComponentInChildren<CollisionComponent>();
        foreach (var collider in _actor.Colliders)
            collisionComponent.IgnoreCollision(collider, ignore: true);
        collisionComponent.SetLayer(_flyingLayer);
        collisionComponent.OnCollisionExitEvent += OnCollisionExit;

        StopCharging();
        return true;
    }

    private void OnCollisionExit(CollisionComponent collisionComponent, Collision collision)
    {
        collisionComponent.OnCollisionExitEvent -= OnCollisionExit;
        collisionComponent.RestoreLayer();
        foreach (var collider in _actor.Colliders)
            collisionComponent.IgnoreCollision(collider, ignore: false);
    }
    public Vector3 GetEffectiveGravity()
    {
        return Physics.gravity * _data.SpeedMultiplier * _data.SpeedMultiplier;
    }
}
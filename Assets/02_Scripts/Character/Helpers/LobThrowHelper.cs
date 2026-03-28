using System;
using UnityEngine;

public class LobThrowHelper
{
    [Serializable]
    public class Data
    {
        public float FixedRange = 8f;    // horizontal distance to landing point
        [Tooltip("1 = normal, 0.5 = arrives in half the time, 2 = twice as slow")]
        public float FlightDuration = 1f; // direct control over total time
    }

    private Data _data;
    private IThrowActor _actor;
    private InteractionHelper _interactionHelper;
    private int _flyingLayer;

    public Data GetData() => _data;

    public LobThrowHelper(Data data, IThrowActor actor, InteractionHelper interactionHelper, int flyingLayer)
    {
        _data = data;
        _actor = actor;
        _interactionHelper = interactionHelper;
        _flyingLayer = flyingLayer;
    }

    public bool CanShowPreview()
    {
        return _interactionHelper.TryGetPickedUpInteraction(out InteractionController interaction)
            && interaction.GetComponent<PaperBallController>()?.IsLobShot == true;
    }

    public bool TryTriggerThrow()
    {
        if (!_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
            return false;

        var paperBall = stoppedInteraction.GetComponent<PaperBallController>();
        if (paperBall == null || !paperBall.IsLobShot) return false;

        _interactionHelper.TryStopInteraction(stoppedInteraction);
        if (stoppedInteraction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
            interactionOwner.OnThrowed();

        _actor.OnThrow(stoppedInteraction.transform);

        // Calculate velocity for fixed range and height
        Vector3 throwDirection = _actor.LookDirection;
        throwDirection.y = 0;
        throwDirection.Normalize();

        Vector3 velocity = CalculateLobVelocity(throwDirection);
        stoppedInteraction.Rigidbody.AddForce(velocity, ForceMode.VelocityChange);

        // Flying layer + collision ignore (same as ThrowHelper)
        CollisionComponent collisionComponent = stoppedInteraction.GetComponentInChildren<CollisionComponent>();
        foreach (var collider in _actor.Colliders)
            collisionComponent.IgnoreCollision(collider, ignore: true);
        collisionComponent.SetLayer(_flyingLayer);
        collisionComponent.OnCollisionExitEvent += OnCollisionExit;

        return true;
    }

    public Vector3 CalculateLobVelocity(Vector3 horizontalDirection)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float totalTime = _data.FlightDuration;
        float vHorizontal = _data.FixedRange / totalTime;
        float vy = g * totalTime / 2f;

        return horizontalDirection * vHorizontal + Vector3.up * vy;
    }


    private void OnCollisionExit(CollisionComponent collisionComponent, Collision collision)
    {
        collisionComponent.OnCollisionExitEvent -= OnCollisionExit;
        collisionComponent.RestoreLayer();
        foreach (var collider in _actor.Colliders)
            collisionComponent.IgnoreCollision(collider, ignore: false);
    }
}
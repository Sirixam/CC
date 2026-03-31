using System;
using UnityEngine;

public class PlaneThrowHelper
{
    [Serializable]
    public class Data
    {
        public float Speed = 1f;
    }
    private Data _data;
    private IThrowActor _actor;
    private InteractionHelper _interactionHelper;
    private int _flyingLayer;


    public PlaneThrowHelper(Data data, IThrowActor actor, InteractionHelper interactionHelper, int flyingLayer)
    {
        _data = data;
        _actor = actor;
        _interactionHelper = interactionHelper;
        _flyingLayer = flyingLayer;
    }

    public Vector3 CalculateVelocity(Vector3 horizontalDirection)
    {
        return horizontalDirection * _data.Speed;
    }

    public bool TryTriggerThrow()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            var paperBall = stoppedInteraction.GetComponent<PaperBallController>();
            if (paperBall == null || !paperBall.IsPlane) return false;

            _interactionHelper.TryStopInteraction(stoppedInteraction);
            if (stoppedInteraction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
                interactionOwner.OnThrowed();

            _actor.OnThrow(stoppedInteraction.transform);

            Vector3 throwDirection = _actor.LookDirection;
            throwDirection.y = 0;
            throwDirection.Normalize();

            stoppedInteraction.Rigidbody.AddForce(throwDirection * _data.Speed, ForceMode.VelocityChange);

            stoppedInteraction.Rigidbody.useGravity = false;

            CollisionComponent collisionComponent = stoppedInteraction.GetComponentInChildren<CollisionComponent>();

            foreach (var collider in _actor.Colliders)
            {
                collisionComponent.IgnoreCollision(collider, ignore: true);
            }
            collisionComponent.SetLayer(_flyingLayer);
            collisionComponent.OnCollisionExitEvent += OnCollisionExit;

            return true;
        }
        return false;
    }

    public bool CanShowPreview()
    {
        return _interactionHelper.TryGetPickedUpInteraction(out InteractionController interaction)
                && interaction.GetComponent<PaperBallController>()?.IsPlane == true;
    }

    private void OnCollisionExit(CollisionComponent collisionComponent, Collision collision)
    {
        collisionComponent.OnCollisionExitEvent -= OnCollisionExit;
        collisionComponent.RestoreLayer();

        // Re-enable gravity so the plane drops
        collisionComponent.GetComponentInParent<Rigidbody>().useGravity = true;

        foreach (var collider in _actor.Colliders)
            collisionComponent.IgnoreCollision(collider, ignore: false);
    }
}
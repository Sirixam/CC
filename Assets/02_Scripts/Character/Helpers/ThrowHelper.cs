using System;
using UnityEngine;

public interface IThrowActor : IActor
{
    Vector3 LookDirection { get; }
    void OnThrow(Transform thrownTransform);
    Collider[] Colliders { get; }
}

public class ThrowHelper
{
    [Serializable]
    public class Data
    {
        public float Speed = 10f; // Meters per second
        public float PitchAngle = 15f; // Degrees
        [Layer]
        public int FlyingLayer;
    }

    private Data _data;
    private InteractionHelper _interactionHelper;
    private IThrowActor _actor;

    public ThrowHelper(IThrowActor actor, Data data, InteractionHelper interactionHelper)
    {
        _actor = actor;
        _data = data;
        _interactionHelper = interactionHelper;
    }

    public bool TryTriggerThrow()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
            _actor.OnThrow(stoppedInteraction.transform);

            Vector3 throwDirection = _actor.LookDirection;
            throwDirection.y = Mathf.Tan(_data.PitchAngle * Mathf.Deg2Rad);
            stoppedInteraction.Rigidbody.AddForce(throwDirection * _data.Speed, ForceMode.VelocityChange);

            CollisionComponent collisionComponent = stoppedInteraction.GetComponentInChildren<CollisionComponent>();
            foreach (var collider in _actor.Colliders)
            {
                collisionComponent.IgnoreCollision(collider, ignore: true);
            }
            collisionComponent.SetLayer(_data.FlyingLayer);
            collisionComponent.OnCollisionExitEvent += OnCollisionExit;
            return true;
        }
        return false;
    }

    private void OnCollisionExit(CollisionComponent collisionComponent, Collision collision)
    {
        collisionComponent.OnCollisionExitEvent -= OnCollisionExit;
        collisionComponent.RestoreLayer();
        foreach (var collider in _actor.Colliders)
        {
            collisionComponent.IgnoreCollision(collider, ignore: false);
        }
    }
}

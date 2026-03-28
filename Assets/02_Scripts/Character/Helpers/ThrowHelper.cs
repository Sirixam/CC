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
        public float SpinForce = 10f;
    }

    private Data _data;
    private IThrowActor _actor;
    private InteractionHelper _interactionHelper;
    private int _flyingLayer;

    public ThrowHelper(Data data, IThrowActor actor, InteractionHelper interactionHelper, int flyingLayer)
    {
        _data = data;
        _actor = actor;
        _interactionHelper = interactionHelper;
        _flyingLayer = flyingLayer;
    }

    public bool CanShowPreview()
    {
        return _interactionHelper.TryGetPickedUpInteraction(out _);
    }

    public bool TryTriggerThrow()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
            if (stoppedInteraction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
            {
                interactionOwner.OnThrowed();
            }

            _actor.OnThrow(stoppedInteraction.transform);

            Vector3 throwDirection = _actor.LookDirection;
            throwDirection.y = Mathf.Tan(_data.PitchAngle * Mathf.Deg2Rad);
            throwDirection.Normalize();

            // Add spin
            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized * _data.SpinForce;
            stoppedInteraction.Rigidbody.AddTorque(randomTorque.normalized * _data.SpinForce, ForceMode.VelocityChange);

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

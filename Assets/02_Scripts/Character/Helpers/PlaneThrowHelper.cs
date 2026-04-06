using System;
using UnityEngine;

public class PlaneThrowHelper
{
    [Serializable]
    public class Data
    {
        public float Speed = 5f;

    }
    private Data _data;
    private IThrowActor _actor;
    private InteractionHelper _interactionHelper;
    private int _flyingLayer;
    public Data GetData() => _data;



    public PlaneThrowHelper(Data data, IThrowActor actor, InteractionHelper interactionHelper, int flyingLayer)
    {
        _data = data;
        _actor = actor;
        _interactionHelper = interactionHelper;
        _flyingLayer = flyingLayer;
    }

    public float GetHeldItemDrag()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController interaction))
            return interaction.Rigidbody.drag;
        return 1f; // fallback
    }

    public Vector3 CalculateThrowVelocity(out Vector3 direction)
    {
        direction = _actor.LookDirection;
        direction.y = 0;
        direction.Normalize();
        return CalculateThrowVelocity(direction);
    }

    public Vector3 CalculateThrowVelocity(Vector3 horizontalDirection)
    {
        float drag = GetHeldItemDrag();
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

            Vector3 velocity = CalculateThrowVelocity(out Vector3 throwDirection);
            stoppedInteraction.Rigidbody.AddForce(velocity, ForceMode.VelocityChange);

            var flight = stoppedInteraction.gameObject.AddComponent<PlaneFlightBehavior>();
            flight.Initialize(_data.Speed, throwDirection);

            CollisionComponent collisionComponent = stoppedInteraction.GetComponentInChildren<CollisionComponent>();

            foreach (var collider in _actor.Colliders)
                collisionComponent.IgnoreCollision(collider, ignore: true);

            collisionComponent.SetLayer(_flyingLayer);

            // Restore layer after a short delay so the plane can collide with environment
            stoppedInteraction.GetComponent<MonoBehaviour>().StartCoroutine(
                DelayedLayerRestore(collisionComponent, stoppedInteraction)
            );
            return true;
        }
        return false;
    }
    private System.Collections.IEnumerator DelayedLayerRestore(
    CollisionComponent collisionComponent,
    InteractionController interaction)
    {
        yield return new WaitForSeconds(0.1f); // enough time to clear player colliders

        if (collisionComponent == null) yield break;

        collisionComponent.RestoreLayer();

        foreach (var collider in _actor.Colliders)
            collisionComponent.IgnoreCollision(collider, ignore: false);
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
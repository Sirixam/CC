using System;
using UnityEngine;

public class CollisionComponent : MonoBehaviour
{
    [Tag]
    [SerializeField] private string[] _validTags;
    [SerializeField] private Collider[] _colliders = new Collider[0];

    private int[] _initialColliderLayers;

    public event Action<CollisionComponent, Collision> OnCollisionEnterEvent;
    public event Action<CollisionComponent, Collision> OnCollisionExitEvent;

    private void Awake()
    {
        _initialColliderLayers = new int[_colliders.Length];
        for (int i = 0; i < _colliders.Length; i++)
        {
            _initialColliderLayers[i] = _colliders[i].gameObject.layer;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!HasValidTag(collision, _validTags)) return;

        OnCollisionEnterEvent?.Invoke(this, collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!HasValidTag(collision, _validTags)) return;

        OnCollisionExitEvent?.Invoke(this, collision);
    }

    public void SetLayer(int layer)
    {
        foreach (var collider in _colliders)
        {
            collider.gameObject.layer = layer;
        }
    }

    public void RestoreLayer()
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].gameObject.layer = _initialColliderLayers[i];
        }
    }

    public void IgnoreCollision(Collider other, bool ignore)
    {
        foreach (var collider in _colliders)
        {
            Physics.IgnoreCollision(collider, other, ignore);
        }
    }

    private bool HasValidTag(Collision collision, string[] tags)
    {
        if (tags.Length == 0) return true;

        Transform other = collision.transform;
        foreach (var tag in tags)
        {
            if (other.CompareTag(tag)) return true;
        }
        return false;
    }
}

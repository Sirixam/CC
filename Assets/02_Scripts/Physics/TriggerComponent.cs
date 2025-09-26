using System;
using UnityEngine;

public class TriggerComponent : MonoBehaviour
{
    [Tag]
    [SerializeField] private string[] _validTags;

    public event Action<TriggerComponent, Collider> OnTriggerEnterEvent;
    public event Action<TriggerComponent, Collider> OnTriggerExitEvent;

    private void OnTriggerEnter(Collider other)
    {
        if (!HasValidTag(other, _validTags)) return;

        OnTriggerEnterEvent?.Invoke(this, other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasValidTag(other, _validTags)) return;

        OnTriggerExitEvent?.Invoke(this, other);
    }

    private bool HasValidTag(Collider other, string[] tags)
    {
        if (tags.Length == 0) return true;

        foreach (var tag in tags)
        {
            if (other.CompareTag(tag)) return true;
        }
        return false;
    }
}

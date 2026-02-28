using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to any GameObject that owns a trigger collider.
/// Wraps OnTriggerEnter/Exit, maintains the active-collider set, and exposes
/// events that any other script can subscribe to without touching physics callbacks.
/// </summary>
public class TriggerListener : MonoBehaviour
{
    [Tag]
    [SerializeField] private string[] _filterTags = { }; // Only pass colliders matching one of these tags.Empty = allow all.
    [SerializeField] private LayerMask _filterLayers = ~0; // Everything by default

    // Raised for every matching collider that enters or exits.
    public event Action<Collider> OnEnter;
    public event Action<Collider> OnExit;

    // Raised once when the first collider enters (true) or the last one exits (false).
    public event Action<bool> OnOccupiedChanged;

    private readonly List<Collider> _active = new();

    public IReadOnlyList<Collider> Active => _active;
    public bool IsOccupied => _active.Count > 0;

    // ── Unity callbacks ───────────────────────────────────────────────────────

    private void OnDisable()
    {
        // Physics won't fire exits for us when disabled, so clear silently.
        _active.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Passes(other)) return;

        bool wasEmpty = _active.Count == 0;
        _active.Add(other);
        OnEnter?.Invoke(other);
        if (wasEmpty)
        {
            OnOccupiedChanged?.Invoke(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_active.Remove(other)) return;

        OnExit?.Invoke(other);
        if (_active.Count == 0)
        {
            OnOccupiedChanged?.Invoke(false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Returns the first active collider whose hierarchy contains <typeparamref name="T"/>.
    public bool TryGetFirst<T>(out T component) where T : Component
    {
        foreach (Collider col in _active)
        {
            if (col == null) continue;
            component = col.GetComponentInParent<T>();
            if (component != null) return true;
        }
        component = default;
        return false;
    }

    /// Fills <paramref name="results"/> with one <typeparamref name="T"/> per active collider
    /// whose hierarchy contains the component. Does not clear the list beforehand.
    public void GetAll<T>(List<T> results) where T : Component
    {
        foreach (Collider col in _active)
        {
            if (col == null) continue;
            T comp = col.GetComponentInParent<T>();
            if (comp != null) results.Add(comp);
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private bool Passes(Collider other)
    {
        if ((_filterLayers.value & (1 << other.gameObject.layer)) == 0) return false;
        if (_filterTags.Length == 0) return true;
        foreach (string tag in _filterTags)
            if (other.CompareTag(tag)) return true;
        return false;
    }
}

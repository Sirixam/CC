using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Random = UnityEngine.Random;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private SpawnDefinition _definition;

    [Tooltip("Parent of points where objects can be spawned. Falls back to this transform if empty.")]
    [SerializeField] private Transform _spawnPointsParent;

    [Tooltip("Optional parent for spawned objects. Defaults to scene root if unassigned.")]
    [SerializeField] private Transform _spawnParent;

    [Tooltip("Start spawning automatically on Enable.")]
    [SerializeField] private bool _autoStart = true;

    private Transform[] _spawnPoints;
    private readonly Dictionary<Transform, GameObject> _occupiedPoints = new();
    private CancellationTokenSource _cts;

    private Transform SpawnPointsParent => _spawnPointsParent != null ? _spawnPointsParent : transform;

    private void OnEnable()
    {
        if (_autoStart)
            StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    // --- Public API ---

    public void StartSpawning(CancellationToken externalToken = default)
    {
        StopSpawning();
        _cts = externalToken == default
            ? new CancellationTokenSource()
            : CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        SpawnLoop(_cts.Token).Forget();
    }

    public void StopSpawning()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void DespawnAll()
    {
        foreach (var obj in _occupiedPoints.Values)
            if (obj != null) Destroy(obj);
        _occupiedPoints.Clear();
    }

    // --- Internal ---

    private async UniTaskVoid SpawnLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            float interval = Random.Range(_definition.IntervalRange.x, _definition.IntervalRange.y);
            await UniTask.WaitForSeconds(interval, cancellationToken: token);

            if (token.IsCancellationRequested) break;

            // Free points whose objects have been destroyed externally
            foreach (var key in new List<Transform>(_occupiedPoints.Keys))
                if (_occupiedPoints[key] == null) _occupiedPoints.Remove(key);

            if (_occupiedPoints.Count >= _definition.MaxActive) continue;

            GameObject prefab = PickWeightedRandom(out Vector3 offset);
            if (prefab == null) continue;

            Transform point = PickFreeSpawnPoint();
            if (point == null) continue;

            GameObject spawned = Instantiate(prefab, point.position + offset, point.rotation, _spawnParent);
            _occupiedPoints[point] = spawned;
        }
    }

    private GameObject PickWeightedRandom(out Vector3 offset)
    {
        if (_definition.Entries == null || _definition.Entries.Length == 0)
        {
            offset = Vector3.zero;
            return null;
        }

        float total = 0f;
        foreach (var entry in _definition.Entries)
            total += entry.Weight;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var entry in _definition.Entries)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
            {
                offset = entry.Offset;
                return entry.Prefab;
            }
        }

        var defaultEntry = _definition.Entries[^1];
        offset = defaultEntry.Offset;
        return defaultEntry.Prefab;
    }

    private Transform PickFreeSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            _spawnPoints = SpawnPointsParent.GetComponentsInChildren<Transform>();

        // Collect points that have no living occupant
        var free = new List<Transform>();
        foreach (var point in _spawnPoints)
            if (point != null && !_occupiedPoints.ContainsKey(point))
                free.Add(point);

        if (free.Count == 0) return null;
        return free[Random.Range(0, free.Count)];
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            _spawnPoints = SpawnPointsParent.GetComponentsInChildren<Transform>();
        }

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
        foreach (var point in _spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, 0.25f);
        }
    }
#endif
}

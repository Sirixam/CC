using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Spawn", menuName = "Definitions/Spawn")]
public class SpawnDefinition : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public GameObject Prefab;
        public Vector3 Offset;
        [Min(0f), Tooltip("Relative probability. Higher = more likely to be picked.")]
        public float Weight = 1f;
    }

    [Tooltip("Prefabs that can be spawned, each with a relative weight.")]
    public Entry[] Entries;

    [Tooltip("Random interval (seconds) between each spawn attempt.")]
    public Vector2 IntervalRange = new Vector2(2f, 5f);

    [Tooltip("Maximum number of simultaneously active spawned objects.")]
    public int MaxActive = 10;
}

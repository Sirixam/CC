using System;
using UnityEngine;

[CreateAssetMenu(fileName = "InputIconConfig", menuName = "Config/Input Icon Config")]
public class InputIconConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public EAction Action;
        public Sprite GamepadSprite;
        public Sprite KeyboardSprite;
    }

    [SerializeField] private Entry[] _entries;

    public Sprite GetSprite(EAction action, EDevice device)
    {
        foreach (var entry in _entries)
        {
            if (entry.Action != action) continue;
            return device == EDevice.Gamepad ? entry.GamepadSprite : entry.KeyboardSprite;
        }
        Debug.LogWarning($"No icon found for action {action} / device {device}");
        return null;
    }
}

using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


[AttributeUsage(AttributeTargets.Field)]
public class ToggleButtonsAttribute : PropertyAttribute
{
    public string OnLabel { get; }
    public string OffLabel { get; }

    public ToggleButtonsAttribute(string onLabel = "ON", string offLabel = "OFF")
    {
        OnLabel = onLabel;
        OffLabel = offLabel;
    }
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(ToggleButtonsAttribute))]
public class ToggleButtonsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Boolean)
        {
            EditorGUI.LabelField(position, label.text, "ToggleButtons only works on bool fields");
            return;
        }

        var toggle = (ToggleButtonsAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);

        // Label
        position = EditorGUI.PrefixLabel(position, label);

        // Button rects
        float halfWidth = position.width / 2f;
        Rect onRect = new Rect(position.x, position.y, halfWidth, position.height);
        Rect offRect = new Rect(position.x + halfWidth, position.y, halfWidth, position.height);

        bool currentValue = property.boolValue;

        // Styles
        GUIStyle onStyle = new GUIStyle(EditorStyles.miniButtonLeft);
        GUIStyle offStyle = new GUIStyle(EditorStyles.miniButtonRight);

        Color prevColor = GUI.backgroundColor;

        if (currentValue)
            GUI.backgroundColor = Color.green;

        if (GUI.Button(onRect, toggle.OnLabel, onStyle))
            property.boolValue = true;

        GUI.backgroundColor = prevColor;

        if (!currentValue)
            GUI.backgroundColor = Color.red;

        if (GUI.Button(offRect, toggle.OffLabel, offStyle))
            property.boolValue = false;

        GUI.backgroundColor = prevColor;

        EditorGUI.EndProperty();
    }
}
#endif

using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class PlayerCountButtonsAttribute : PropertyAttribute { }

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(PlayerCountButtonsAttribute))]
public class PlayerCountButtonsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.LabelField(position, label.text, "PlayerCountButtons only works on int fields");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, label);

        float halfWidth = position.width / 2f;
        Rect oneRect = new Rect(position.x, position.y, halfWidth, position.height);
        Rect twoRect = new Rect(position.x + halfWidth, position.y, halfWidth, position.height);

        int current = property.intValue;
        Color prevColor = GUI.backgroundColor;

        GUI.backgroundColor = current == 1 ? Color.green : prevColor;
        if (GUI.Button(oneRect, "1 Player", EditorStyles.miniButtonLeft))
            property.intValue = 1;

        GUI.backgroundColor = current == 2 ? Color.green : prevColor;
        if (GUI.Button(twoRect, "2 Players", EditorStyles.miniButtonRight))
            property.intValue = 2;

        GUI.backgroundColor = prevColor;

        EditorGUI.EndProperty();
    }
}

#endif

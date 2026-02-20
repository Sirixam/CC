using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(TestDefinition))]
public class TestDefinitionEditor : Editor
{
    private static readonly Color DefaultColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    private static readonly Color NormalColor = Color.white;

    private SerializedProperty _correctnessProp;
    private SerializedProperty _forcedNpcAnswerProp;
    private SerializedProperty _forcedDistractionLevelProp;

    private void OnEnable()
    {
        _correctnessProp = serializedObject.FindProperty("_correctness");
        _forcedNpcAnswerProp = serializedObject.FindProperty("ForcedNpcAnswer");
        _forcedDistractionLevelProp = serializedObject.FindProperty("ForcedDistractionLevel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var active = new List<Entry>();
        var unused = new List<Entry>();

        AddEntry(_correctnessProp, _correctnessProp.enumValueIndex == 0, active, unused);
        AddEntry(_forcedNpcAnswerProp, _forcedNpcAnswerProp.objectReferenceValue == null, active, unused);
        AddEntry(_forcedDistractionLevelProp, _forcedDistractionLevelProp.intValue == 0, active, unused);

        // Draw active first
        foreach (var entry in active)
            DrawWithColor(entry.Property, false);

        // Draw unused section if needed
        if (unused.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Unused Fields", EditorStyles.boldLabel);
            foreach (var entry in unused)
                DrawWithColor(entry.Property, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddEntry(
        SerializedProperty property,
        bool isDefault,
        List<Entry> active,
        List<Entry> unused)
    {
        if (isDefault)
            unused.Add(new Entry(property, true));
        else
            active.Add(new Entry(property, false));
    }

    private void DrawWithColor(SerializedProperty property, bool isDefault)
    {
        Color previous = GUI.color;
        GUI.color = isDefault ? DefaultColor : NormalColor;

        EditorGUILayout.PropertyField(property);

        GUI.color = previous;
    }

    private struct Entry
    {
        public SerializedProperty Property;
        public bool IsDefault;

        public Entry(SerializedProperty property, bool isDefault)
        {
            Property = property;
            IsDefault = isDefault;
        }
    }
}
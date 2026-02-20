using UnityEditor;
using UnityEngine;

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

        DrawCorrectness();
        DrawForcedNpcAnswer();
        DrawForcedDistraction();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCorrectness()
    {
        DrawWithColor(
            isDefault: _correctnessProp.enumValueIndex == 0, // Undefined
            () => EditorGUILayout.PropertyField(_correctnessProp)
        );
    }

    private void DrawForcedNpcAnswer()
    {
        DrawWithColor(
            isDefault: _forcedNpcAnswerProp.objectReferenceValue == null,
            () => EditorGUILayout.PropertyField(_forcedNpcAnswerProp)
        );
    }

    private void DrawForcedDistraction()
    {
        DrawWithColor(
            isDefault: _forcedDistractionLevelProp.intValue == 0,
            () => EditorGUILayout.PropertyField(_forcedDistractionLevelProp)
        );
    }

    private void DrawWithColor(bool isDefault, System.Action drawAction)
    {
        Color previous = GUI.color;
        GUI.color = isDefault ? DefaultColor : NormalColor;

        drawAction.Invoke();

        GUI.color = previous;
    }
}
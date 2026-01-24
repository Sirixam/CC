using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

public class HideIfAttribute : PropertyAttribute
{
    public readonly string ConditionMemberName;

    public HideIfAttribute(string conditionMemberName)
    {
        ConditionMemberName = conditionMemberName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HideIfAttribute))]
public class HideIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldHide(property))
            return;

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return ShouldHide(property)
            ? 0f
            : EditorGUI.GetPropertyHeight(property, label, true);
    }

    private bool ShouldHide(SerializedProperty property)
    {
        HideIfAttribute hideIf = (HideIfAttribute)attribute;
        object target = property.serializedObject.targetObject;
        System.Type type = target.GetType();

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        // 1. Method
        MethodInfo method = type.GetMethod(hideIf.ConditionMemberName, flags);
        if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
        {
            return (bool)method.Invoke(target, null);
        }

        // 2. Property
        PropertyInfo prop = type.GetProperty(hideIf.ConditionMemberName, flags);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            return (bool)prop.GetValue(target);
        }

        // 3. Field
        FieldInfo field = type.GetField(hideIf.ConditionMemberName, flags);
        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(target);
        }

        Debug.LogWarning(
            $"HideIf: Could not find a bool field, property, or parameterless method named '{hideIf.ConditionMemberName}' on {type.Name}");

        return false;
    }
}
#endif

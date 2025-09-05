using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
#endif

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string Label { get; }

    public ButtonAttribute(string label = null)
    {
        Label = label;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(MonoBehaviour), true)]
public class ButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw default fields

        var type = target.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<ButtonAttribute>();
            if (attribute != null)
            {
                string buttonLabel = attribute.Label ?? method.Name;
                if (GUILayout.Button(buttonLabel))
                {
                    method.Invoke(target, null);
                }
            }
        }
    }
}

#endif
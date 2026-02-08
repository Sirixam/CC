using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
#endif

public enum ButtonColor
{
    Default,
    Red,
    Green,
    Blue,
    Yellow
}

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string Label { get; }
    public ButtonColor Color { get; }

    public ButtonAttribute(string label = null, ButtonColor color = ButtonColor.Default)
    {
        Label = label;
        Color = color;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(MonoBehaviour), true)]
public class ButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var type = target.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<ButtonAttribute>();
            if (attribute != null)
            {
                Color originalColor = GUI.backgroundColor;
                string buttonLabel = attribute.Label ?? method.Name;
                if (attribute.Color != ButtonColor.Default)
                {
                    GUI.backgroundColor = GetBackgroundColor(attribute.Color);
                }
                if (GUILayout.Button(buttonLabel))
                {
                    method.Invoke(target, null);
                }
                GUI.backgroundColor = originalColor;
            }
        }
    }

    private Color GetBackgroundColor(ButtonColor type) => type switch
    {
        ButtonColor.Red => Color.red,
        ButtonColor.Green => Color.green,
        ButtonColor.Blue => Color.blue,
        ButtonColor.Yellow => Color.yellow,
        _ => GUI.color
    };
}

#endif
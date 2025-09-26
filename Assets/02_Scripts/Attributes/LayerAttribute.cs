using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LayerAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(LayerAttribute))]
public class LayerAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
        else if (property.propertyType == SerializedPropertyType.String)
        {
            // Convert string to layer index
            int index = LayerMask.NameToLayer(property.stringValue);
            index = EditorGUI.LayerField(position, label, index);
            property.stringValue = LayerMask.LayerToName(index);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use Layer with int or string.");
        }

        EditorGUI.EndProperty();
    }
}
#endif

using NeutronNetwork;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableColor))]
public class NeutronColorDrawer : PropertyDrawer
{
    private Color serializableColor;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float r = property.FindPropertyRelative("r").floatValue;
        float g = property.FindPropertyRelative("g").floatValue;
        float b = property.FindPropertyRelative("b").floatValue;
        float a = property.FindPropertyRelative("a").floatValue;
        serializableColor = new Color(r, g, b);
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        serializableColor = EditorGUI.ColorField(position, string.Empty, serializableColor);
        EditorGUI.EndProperty();
        property.FindPropertyRelative("r").floatValue = serializableColor.r;
        property.FindPropertyRelative("g").floatValue = serializableColor.g;
        property.FindPropertyRelative("b").floatValue = serializableColor.b;
        property.FindPropertyRelative("a").floatValue = serializableColor.a;
    }
}
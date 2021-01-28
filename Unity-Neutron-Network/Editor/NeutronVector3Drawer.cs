using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableVector3))]
public class NeutronVector3Drawer : PropertyDrawer
{
    private Vector3 serializableVector3;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float x = property.FindPropertyRelative("x").floatValue;
        float y = property.FindPropertyRelative("y").floatValue;
        float z = property.FindPropertyRelative("z").floatValue;
        serializableVector3 = new Vector3(x, y, z);
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        serializableVector3 = EditorGUI.Vector3Field(position, string.Empty, serializableVector3);
        EditorGUI.EndProperty();
        property.FindPropertyRelative("x").floatValue = serializableVector3.x;
        property.FindPropertyRelative("y").floatValue = serializableVector3.y;
        property.FindPropertyRelative("z").floatValue = serializableVector3.z;
    }
}
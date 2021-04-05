using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NeutronAnimatorParameter))]
public class NeutronAnimatorParameterDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty parameterMode = property.FindPropertyRelative("parameterMode");
        SerializedProperty parameterName = property.FindPropertyRelative("parameterName");
        SerializedProperty parameterType = property.FindPropertyRelative("parameterType");
        int indexEnumValue = parameterType.intValue;
        AnimatorControllerParameterType paramaterTypeName = (AnimatorControllerParameterType)indexEnumValue;
        EditorGUI.PropertyField(position, parameterMode, new GUIContent($"n: {parameterName.stringValue} | t: {paramaterTypeName.ToString()}"));
    }
}
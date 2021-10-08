using NeutronNetwork.Editor;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimatorParameter))]
public class NeutronAnimatorParameterDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty parameterMode = property.FindPropertyRelative("_syncMode");
        SerializedProperty parameterName = property.FindPropertyRelative("_parameterName");
        SerializedProperty parameterType = property.FindPropertyRelative("_parameterType");
        int indexEnumValue = parameterType.intValue;
        AnimatorControllerParameterType paramaterTypeName = (AnimatorControllerParameterType)indexEnumValue;
        EditorGUI.PropertyField(position, parameterMode, new GUIContent($"n: {parameterName.stringValue} | t: {paramaterTypeName.ToString()}"));
    }
}
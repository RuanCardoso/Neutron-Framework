using System;
using System.Collections;
using System.Collections.Generic;
using Supyrb;
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
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 50, position.height), parameterMode, new GUIContent($"n: {parameterName.stringValue} | t: {paramaterTypeName.ToString()}"));
    }
}
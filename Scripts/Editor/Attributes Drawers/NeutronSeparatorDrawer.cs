using NeutronNetwork.Attributes;
using NeutronNetwork.Naughty.Attributes;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SeparatorAttribute))]
public class NeutronSeparatorDrawer : PropertyDrawer
{
    const int FIXED_HEIGHT = 20;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            if (!EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, position.height - FIXED_HEIGHT), property, label, true))
                EditorGUI.LabelField(new Rect(position.x, position.y + FIXED_HEIGHT, position.width, position.height), "", GUI.skin.horizontalSlider);
        }
        else EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, position.height), property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label) + FIXED_HEIGHT;
    }
}
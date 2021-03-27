using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SeparatorAttribute))]
public class NeutronSeparatorDrawer : PropertyDrawer
{
    GUIStyle prefixStyle = new GUIStyle();
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        prefixStyle.richText = true;
        prefixStyle.normal.textColor = Color.white;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, position.height - 20), property, label);
        EditorGUI.LabelField(new Rect(position.x, position.y + 20, position.width, position.height), "", GUI.skin.horizontalSlider);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label) + 20;
    }
}
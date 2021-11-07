using NeutronNetwork.Attributes;
using NeutronNetwork.Naughty.Attributes.Editor;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HorizontalLineDownAttribute))]
public class NeutronHorizontalLineDrawer : PropertyDrawer
{
    private Color32 Gray = new Color32(128, 128, 128, 255);
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        Rect controlRect = EditorGUILayout.GetControlRect();
        if (controlRect != null)
        {
            controlRect.height = 2;
            controlRect.y += controlRect.height * 4;
            NaughtyEditorGUI.HorizontalLine(controlRect, 2.0f, Gray);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
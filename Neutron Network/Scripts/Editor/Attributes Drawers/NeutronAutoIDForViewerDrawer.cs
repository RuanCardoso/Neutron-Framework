using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(IDAttribute))]
public class NeutronAutoIDForViewerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!EditorGUI.PropertyField(position, property, label) && property.intValue == 0)
            property.intValue = Mathf.Abs(property.serializedObject.targetObject.GetInstanceID());
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
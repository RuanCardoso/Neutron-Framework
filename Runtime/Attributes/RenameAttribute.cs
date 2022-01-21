using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class RenameAttribute : PropertyAttribute
{
    public string label;
    public RenameAttribute(string label)
    {
        this.label = label;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RenameAttribute))]
    public class ThisPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, new GUIContent((attribute as RenameAttribute).label), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
    #endif
}
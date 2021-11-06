using NeutronNetwork;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SyncVarAttribute), true)]
public class NeutronSynchronizeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Texture2D icongreen = Resources.Load<Texture2D>("Icons/sync");
        Texture2D iconred = Resources.Load<Texture2D>("Icons/syncr");
        label.image = fieldInfo.IsPrivate ? iconred : icongreen;
        label.text = $" | {label.text}";
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
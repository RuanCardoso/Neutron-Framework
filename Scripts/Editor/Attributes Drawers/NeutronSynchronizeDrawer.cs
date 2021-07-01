using NeutronNetwork;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SyncAttribute), true)]
public class NeutronSynchronizeDrawer : PropertyDrawer
{
    GUIStyle prefixStyle = new GUIStyle();
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        prefixStyle.richText = true;
        prefixStyle.normal.textColor = GUI.skin.label.normal.textColor;
        EditorGUI.PrefixLabel(position, new GUIContent($"{label.text}<size=10><color=green><b><i>[Synced]</i></b></color></size>"), prefixStyle);
        EditorGUI.PropertyField(position, property, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
using NeutronNetwork;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SyncVarAttribute), true)]
public class NeutronSynchronizeDrawer : PropertyDrawer
{
    private Texture2D m_Texture = new Texture2D(4, 4);
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Color[] colors = new Color[3];
        colors[0] = Color.green;
        colors[1] = Color.green;
        colors[2] = Color.green;
        int mipCount = Mathf.Min(3, m_Texture.mipmapCount);

        // tint each mip level
        for (int mip = 0; mip < mipCount; ++mip)
        {
            Color[] cols = m_Texture.GetPixels(mip);
            for (int i = 0; i < cols.Length; ++i)
            {
                cols[i] = Color.Lerp(cols[i], colors[mip], 0.33f);
            }
            m_Texture.SetPixels(cols, mip);
        }
        // actually apply all SetPixels, don't recalculate mip levels
        m_Texture.Apply(false);

        label.image = m_Texture;
        label.text = $" - {label.text}";
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
using NeutronNetwork.Internal.Attributes;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        GUI.enabled = false;
    }

    public override float GetHeight()
    {
        return 0;
    }
}
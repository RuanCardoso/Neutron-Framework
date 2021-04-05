using NeutronNetwork.Internal.Attributes;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DisableFieldAttribute))]
public class NeutronDisableFieldDrawer : DecoratorDrawer
{
    public override float GetHeight()
    {
        return 0;
    }

    public override void OnGUI(Rect position)
    {
        GUI.enabled = false;
    }
}
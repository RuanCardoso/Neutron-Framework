using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HelpBoxStyle : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUI.skin.GetStyle("HelpBox").fontSize = 13;
    }
}

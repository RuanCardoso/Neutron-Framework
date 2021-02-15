// using NeutronNetwork;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;

// [CustomEditor(typeof(NeutronSyncBehaviour), true)]
// public class NeutronSync : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         base.OnInspectorGUI();

//         GUI.skin.GetStyle("HelpBox").fontSize = 13;

//         NeutronSyncBehaviour eTarget = (NeutronSyncBehaviour)target;
//         FieldInfo[] fieldInfos = eTarget.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//         int count = fieldInfos.Count(x => x.GetCustomAttribute<SyncVarAttribute>() != null && x.FieldType.IsGenericType);
//         if (count > 0) EditorGUILayout.HelpBox("Note: By default, collections are not synchronized through edits in the inspector.\r\n\r\nex: ObservableList<Type>\r\n\r\nTo synchronize collections by the inspector implements OnValidate();", MessageType.Info);
//     }
// }
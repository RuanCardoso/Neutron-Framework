using NeutronNetwork.Editor;
using UnityEditor;
using UnityEngine;

public class SubScenesEditor : EditorWindow
{
    private SubSceneList _subSceneList;
    private SerializedObject _serializedObject;
    private SerializedProperty _propertyList;
    private Vector2 _scrollView;

    [MenuItem("Neutron/Sub-Scenes", priority = -10)]
    static void Init()
    {
        EditorWindow Window = GetWindow(typeof(SubScenesEditor), true, "Sub-Scenes");
        if (Window != null)
        {
            Window.maxSize = new Vector2(480, 515);
            Window.minSize = new Vector2(480, 515);
        }
    }

    private void OnEnable()
    {
        _subSceneList = FindObjectOfType<SubSceneList>();
        if (_subSceneList != null)
        {
            _serializedObject = new SerializedObject(_subSceneList);
            _propertyList = _serializedObject.FindProperty("_subScenes");
        }
    }

    private void OnGUI()
    {
        if (_serializedObject != null)
        {
            _serializedObject.Update();
            EditorGUILayout.BeginVertical();
            _scrollView = EditorGUILayout.BeginScrollView(_scrollView);
            EditorGUILayout.PropertyField(_propertyList, true);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            //_serializedObject.ApplyModifiedProperties();
        }
        else
            EditorGUILayout.LabelField("No Sub-Scenes found");
    }
}
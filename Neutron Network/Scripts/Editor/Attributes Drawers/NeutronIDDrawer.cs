using UnityEngine;
using UnityEditor;
using NeutronNetwork;
using System;
using UnityEditor.Experimental.SceneManagement;
using Random = UnityEngine.Random;
using System.Linq;
using NeutronNetwork.Internal.Attributes;

[CustomPropertyDrawer(typeof(IDAttribute))]
public class NeutronIDDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        IDAttribute attr = attribute as IDAttribute;
        MonoBehaviour targetObject = (MonoBehaviour)property.serializedObject.targetObject;
        if (!EditorGUI.PropertyField(position, property, label) && !Application.isPlaying)
        {
            Type type = targetObject.GetType();
            if (type.IsSubclassOf(typeof(NeutronBehaviour)) && property.intValue == 0)
                property.intValue = GenerateID(targetObject);
            else if (type.IsAssignableFrom(typeof(NeutronView)))
            {
                if (PrefabStageUtility.GetCurrentPrefabStage() == null)
                {
                    if (targetObject.gameObject.activeInHierarchy && property.intValue != GenerateID(targetObject))
                        property.intValue = GenerateID(targetObject);
                    else if (!targetObject.gameObject.activeInHierarchy && property.intValue != 0) property.intValue = 0;
                }
            }
            else if (property.intValue == 0) Debug.LogError("Type not supported auto ID");
        }
    }

    private int GenerateID(MonoBehaviour monoBehaviour)
    {
        return Mathf.Abs(monoBehaviour.GetInstanceID());
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
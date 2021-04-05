using UnityEngine;
using UnityEditor;
using NeutronNetwork;
using System;
using UnityEditor.Experimental.SceneManagement;
using Random = UnityEngine.Random;
using System.Linq;

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
                property.intValue = Mathf.Abs(targetObject.GetInstanceID());
            else if (type.IsAssignableFrom(typeof(NeutronView)))
            {
                if (!PrefabStageUtility.GetCurrentPrefabStage())
                {
                    if (targetObject.gameObject.activeInHierarchy && property.intValue == 0)
                        property.intValue = Random.Range(1, (2771 - 1));
                    else if (targetObject.gameObject.activeInHierarchy && property.intValue != 0)
                    {
                        NeutronView[] neutronViews = GameObject.FindObjectsOfType<NeutronView>();
                        int count = neutronViews.Count(x => x.ID == property.intValue);
                        if (count > 1)
                            property.intValue = Random.Range(1, (2771 - 1));
                    }
                    else if (!targetObject.gameObject.activeInHierarchy && property.intValue != 0) property.intValue = 0;
                }
            }
            else if (property.intValue == 0) Debug.LogError("Type not supported auto ID");
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
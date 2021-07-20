using UnityEditor;
using UnityEngine;
using NeutronNetwork.Server.Internal;
using UnityEditor.SceneManagement;
using NeutronNetwork.Server;

public class NeutronEditor : EditorWindow
{
    [MenuItem("Neutron/Settings/Neutron")]
    private static void OpenSettings()
    {
        Object asset = Resources.Load("Neutron Settings");
        if (asset != null)
            AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Neutron/Settings/Synchronization")]
    private static void OpenSynchronization()
    {
        Object asset = Resources.Load("Neutron Synchronization");
        if (asset != null)
            AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Neutron/Setup")]
    private static void Setup()
    {
        GameObject l_Controllers = GameObject.Find("Controllers");
        if (l_Controllers == null)
        {
            l_Controllers = new GameObject("Controllers");
            GameObject l_Client = new GameObject("Client");
            GameObject l_Server = new GameObject("Server");
            l_Client.transform.SetParent(l_Controllers.transform);
            l_Server.transform.SetParent(l_Controllers.transform);
            l_Server.AddComponent<NeutronServer>();
            EditorUtility.SetDirty(l_Controllers);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        else Debug.LogError("A setup object has already been created.");
    }

    [MenuItem("Neutron/Documentation")]
    private static void Help()
    {
        EditorUtility.DisplayDialog("Neutron", "Documentation will be released soon.", "OK");
    }
}
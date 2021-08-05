using NeutronNetwork;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Server;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NeutronEditor : EditorWindow
{
    [MenuItem("Neutron/Play(Performance Mode)", priority = -50)]
    private static void Play()
    {
        NeutronModule.EditorLoadSettings().GlobalSettings.PerfomanceMode = true;
        ///////////////////////////////////////////////////////////////////////////
        EditorApplication.isPlaying = !EditorApplication.isPlaying;
    }

    [MenuItem("Neutron/Settings/File/Neutron &F11")]
    private static void OpenSettings()
    {
        Object asset = Resources.Load("Neutron Settings");
        if (asset != null)
        {
            if (AssetDatabase.OpenAsset(asset))
                EditorGUIUtility.PingObject(asset);
        }
    }

    [MenuItem("Neutron/Settings/File/Synchronization &F10")]
    private static void OpenSynchronization()
    {
        Object asset = Resources.Load("Neutron Synchronization");
        if (asset != null)
        {
            if (AssetDatabase.OpenAsset(asset))
                EditorGUIUtility.PingObject(asset);
        }
    }

    [MenuItem("Neutron/Settings/Setup", priority = 0)]
    private static void Setup()
    {
        GameObject l_Controllers = GameObject.Find("Controllers");
        if (l_Controllers == null)
        {
            l_Controllers = new GameObject("Controllers");
            GameObject l_Client = new GameObject("Client");
            GameObject l_Server = new GameObject("Server");
            GameObject l_Custom = new GameObject("Custom");
            l_Client.transform.SetParent(l_Controllers.transform);
            l_Server.transform.SetParent(l_Controllers.transform);
            l_Custom.transform.SetParent(l_Controllers.transform);
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
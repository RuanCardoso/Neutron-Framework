using NeutronNetwork;
using NeutronNetwork.Editor;
using NeutronNetwork.Examples.System.Default;
using NeutronNetwork.Server;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NeutronEditor : EditorWindow
{
    private static Process _clumsy;

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

    [MenuItem("Neutron/Settings/File/Converters")]
    private static void OpenConverters()
    {
        Object asset = Resources.Load("Neutron Converters");
        if (asset != null)
        {
            if (AssetDatabase.OpenAsset(asset))
                EditorGUIUtility.PingObject(asset);
        }
    }

    [MenuItem("Neutron/Settings/Setup/Controllers", priority = 0)]
    private static void Setup()
    {
        GameObject l_Controllers = GameObject.Find("Controllers");
        if (l_Controllers == null)
        {
            l_Controllers = new GameObject("Controllers");
            GameObject l_Client = new GameObject("Client");
            GameObject l_Server = new GameObject("Server");
            GameObject l_Custom = new GameObject("Defines");
            l_Controllers.AddComponent<SubSceneList>();
            l_Client.transform.SetParent(l_Controllers.transform);
            l_Server.transform.SetParent(l_Controllers.transform);
            l_Custom.transform.SetParent(l_Controllers.transform);
            l_Server.AddComponent<NeutronServer>();
            EditorUtility.SetDirty(l_Controllers);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        else
            UnityEngine.Debug.LogError("A setup object has already been created.");
    }

    [MenuItem("Neutron/Settings/Setup/Default Defines", priority = 0)]
    private static void SetupDefaultDefines()
    {
        GameObject l_Controllers = GameObject.Find("Controllers").transform.GetChild(2).gameObject;
        if (l_Controllers != null)
        {
            l_Controllers.AddComponent<InternGlobalController>();
            l_Controllers.AddComponent<InternServerController>();
            EditorUtility.SetDirty(l_Controllers);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        else
            UnityEngine.Debug.LogError("Controllers object not found!");
    }

    [MenuItem("Neutron/Settings/Tools/Lag Simulation &F12")]
    private static void Lag()
    {
        string clumsyFilters = string.Empty;
        if (_clumsy != null && !_clumsy.HasExited)
        {
            _clumsy.Kill();
            LogHelper.Info("Lag simulation restarted!");
        }

        var reference = Resources.Load<TextAsset>("clumsyref");
        string clumsyPath = AssetDatabase.GetAssetPath(reference);
        clumsyPath = clumsyPath.Replace($"{reference.name}.txt", string.Empty);
        clumsyPath = clumsyPath.Replace($"Assets", string.Empty);
        clumsyPath = string.Concat(Application.dataPath, clumsyPath);
        clumsyPath = string.Concat(clumsyPath, "Clumsy/clumsy.exe");
        clumsyPath = Path.GetFullPath(clumsyPath);

        int protocolId = EditorUtility.DisplayDialogComplex("Neutron", "Which protocol do you want to simulate lag?", "Tcp", "Udp", "Both");
        if (protocolId == 0)
            clumsyFilters = NeutronServer.filter_tcp_client_server.ToString();
        else if (protocolId == 1)
            clumsyFilters = NeutronServer.filter_udp_client_server.ToString();
        else if (protocolId == 2)
            clumsyFilters = NeutronServer.filter_tcp_udp_client_server.ToString();
        if (protocolId != 2)
        {
            int sideId = EditorUtility.DisplayDialogComplex("Neutron", "Do you want to simulate lag on server or client outbound?", "Server", "Client", "Both");
            if (sideId == 0)
            {
                if (protocolId == 0)
                    clumsyFilters = NeutronServer.filter_tcp_server.ToString();
                else if (protocolId == 1)
                    clumsyFilters = NeutronServer.filter_udp_server.ToString();
            }
            else if (sideId == 1)
            {
                if (protocolId == 0)
                    clumsyFilters = NeutronServer.filter_tcp_client.ToString();
                else if (protocolId == 1)
                    clumsyFilters = NeutronServer.filter_udp_client.ToString();
            }
            else if (sideId == 2)
            {
                if (protocolId == 0)
                    clumsyFilters = NeutronServer.filter_tcp_client_server.ToString();
                else if (protocolId == 1)
                    clumsyFilters = NeutronServer.filter_udp_client_server.ToString();
            }
        }
        clumsyFilters = clumsyFilters.Replace(System.Environment.NewLine, string.Empty);
        if (clumsyFilters == string.Empty)
            clumsyFilters = "Neutron (:";

        ProcessStartInfo info = new ProcessStartInfo(clumsyPath)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = $"--filter \"{clumsyFilters}\" --lag on --lag-inbound off --lag-time 20"
        };

        if (File.Exists(clumsyPath))
            _clumsy = Process.Start(info);
        else
            EditorUtility.DisplayDialog("Neutron", $"Clumsy not found! {clumsyPath}", "OK");
    }

    [MenuItem("Neutron/Documentation")]
    private static void Help()
    {
        EditorUtility.DisplayDialog("Neutron", "Documentation will be released soon.", "OK");
    }
}
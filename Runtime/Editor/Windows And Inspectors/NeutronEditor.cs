using NeutronNetwork;
using NeutronNetwork.Editor;
using NeutronNetwork.Examples.System.Default;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Server;
using NeutronNetwork.UI;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Draw the Neutron Editor Window.
/// </summary>
public class NeutronEditor : EditorWindow
{
    private const string NEUTRON_NAME = "Neutron Controllers"; // Name of the game object that contains the Neutron controllers.
    private static Process _clumsy; // The lag simulator.

    [MenuItem("Neutron/Settings/File/Neutron &F11")] // Draw a menu item.
    private static void OpenSettings()
    {
        Object asset = Resources.Load<CurrentSettings>("Current Settings");
        if (asset != null)
        {
            if (AssetDatabase.OpenAsset(asset))
                EditorGUIUtility.PingObject(asset); // Open the asset.
        }
    }

    // [MenuItem("Neutron/Settings/File/Synchronization &F10")]
    // private static void OpenSynchronization()
    // {
    //     NeutronNetwork.Helpers.Helper.SetDefines(true, "A");
    // }

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

    [MenuItem("Neutron/Settings/Setup/Neutron Controllers", priority = 0)]
    private static void Setup()
    {
        GameObject control = GameObject.Find(NEUTRON_NAME); // Find the root of the Neutron controllers.
        if (control == null)
        {
            control = new GameObject(NEUTRON_NAME);
            if (control != null)
            {
                GameObject mainControl = new GameObject("Main");
                //mainControl.tag = "mainControl";
                GameObject clientControl = new GameObject("Client(Your Client-Side scripts)");
                //clientControl.tag = "clientControl";
                GameObject serverControl = new GameObject("Server(Your Server-Side scripts)");
                //serverControl.tag = "serverControl";

                mainControl.transform.SetParent(control.transform);
                clientControl.transform.SetParent(control.transform);
                serverControl.transform.SetParent(control.transform);

                mainControl.AddComponent<NeutronServer>();

                control.AddComponent<NeutronModule>();
                control.AddComponent<NeutronSchedule>();
                control.AddComponent<NeutronFramerate>();
                control.AddComponent<NeutronStatistics>();
                control.AddComponent<NeutronInterface>();

                control.AddComponent<NeutronScenes>();

                EditorUtility.SetDirty(control);

                if (EditorUtility.DisplayDialog("Neutron", "Do you want to add the default Neutron controllers?", "Yes", "No"))
                    SetupDefaultControllers(serverControl);
                else
                    EditorUtility.DisplayDialog("Neutron", "You must create your controllers for Neutron to work correctly, see the documentation.", "OK");

                if (EditorUtility.DisplayDialog("Neutron", "Do you want to save the scene?", "Yes", "No"))
                    EditorSceneManager.SaveOpenScenes();
            }
        }
        else
            UnityEngine.Debug.LogError("A setup object has already been created.");
    }

    // [MenuItem("Neutron/Settings/Setup/Default Defines", priority = 0)]
    private static void SetupDefaultControllers(GameObject control)
    {
        control.AddComponent<InternGlobalController>();
        control.AddComponent<InternServerController>();
    }

    [MenuItem("Neutron/Settings/Tools/Lag Simulation &F12")]
    private static void Lag()
    {
        string clumsyFilters = string.Empty;
        if (_clumsy != null && !_clumsy.HasExited)
            _clumsy.Kill();

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
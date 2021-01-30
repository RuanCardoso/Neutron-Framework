using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class NeutronEditor : EditorWindow
{
    bool onRPCViewerLoaded = false;
    int windowSelected = 0;
    //------------------------------------------------------------------------------------------------------------
    List<MethodInfo[]> viewers = new List<MethodInfo[]>();
    //------------------------------------------------------------------------------------------------------------
    Dictionary<string, object> duplicateEntrys = new Dictionary<string, object>();
    //------------------------------------------------------------------------------------------------------------
    [SerializeField] Compression compressionOptions;
    //------------------------------------------------------------------------------------------------------------
    [SerializeField] int serverPort = 5055, voicePort = 5056, backLog = 10, FPS = 45, DPF = 30, sendRate = 3;
    [SerializeField] bool quickPackets, noDelay, antiCheat = true, dontDestroyOnLoad = true, UDPDontFragment = true;
    [SerializeField] int speedHackTolerance = 10, teleportTolerance = 15;
    [SerializeField] string loginUri = "/Network/login.php";

    [MenuItem("Neutron/Neutron Overview")]
    static void Config()
    {
        var editorAsm = typeof(Editor).Assembly;
        var inspWndType = editorAsm.GetType("UnityEditor.InspectorWindow");
        GetWindow<NeutronEditor>("Neutron Overview", true, inspWndType);
    }
    Vector2 scroll;
    private void OnGUI()
    {
        GUIStyle skinToolbar = ((GUISkin)Resources.Load("Skin/Toolbar", typeof(GUISkin))).GetStyle("TextField");
        //--------------------------------------------------------------------------------------------------------------
        windowSelected = GUILayout.Toolbar(windowSelected, new string[] { "Overview", "PC Viewer" }, skinToolbar);
        //--------------------------------------------------------------------------------------------------------------
        if (windowSelected == 1)
        {
            if (GUILayout.Button("Force Refresh"))
            {
                onRPCViewerLoaded = false;
                FindViewers();
            }
        }
        //--------------------------------------------------------------------------------------------------------------
        switch (windowSelected)
        {
            case 0:
                OnOverview();
                break;
            case 1:
                FindViewers();
                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < viewers.Count; i++)
                {
                    duplicateEntrys.Clear();
                    foreach (var mI in viewers[i])
                    {
                        GUIStyle styleText = new GUIStyle(GUI.skin.textArea);
                        styleText.fontStyle = FontStyle.Bold;
                        styleText.alignment = TextAnchor.MiddleLeft;
                        styleText.richText = true;
                        styleText.wordWrap = true;

                        var parametersInfor = mI.CustomAttributes.ToArray();
                        var attrName = parametersInfor[0].AttributeType.Name;
                        var value = (int)parametersInfor[0].ConstructorArguments.First().Value;
                        bool duplicated = false;
                        try
                        {
                            duplicateEntrys.Add($"{value}:{attrName}", mI);
                        }
                        catch { duplicated = true; }
                        string sDuplicated = (duplicated) ? "<color=red> | [DUPLICATED]</color>" : "";
                        EditorGUILayout.LabelField($"[<color=#c2c2c2>Method</color>]: {mI.Name}{sDuplicated} | (ID: <color=#f7382a>{value}</color>) | <color=#26c7fc>{attrName}</color>\r\n[<color=#a3a3a3>Class</color>]: <color=#f5c118>{mI.DeclaringType.Name}</color>", styleText, GUILayout.Height(50));
                    }
                }
                EditorGUILayout.EndScrollView();
                break;
        }
    }

    private void OnDestroy()
    {
        onRPCViewerLoaded = false;
    }

    void OnOverview()
    {
        onRPCViewerLoaded = false;
        //------------------------------------------------------------------------------------
        GUIStyle styleText = new GUIStyle(GUI.skin.label);
        styleText.fontStyle = FontStyle.Bold;
        //------------------------------------------------------------------------------------
        EditorGUILayout.LabelField("Server Settings", styleText);
        compressionOptions = (Compression)EditorGUILayout.EnumPopup("Compression Mode", compressionOptions);
        serverPort = EditorGUILayout.IntField("Server Port", serverPort);
        voicePort = EditorGUILayout.IntField("Voice Port", voicePort);
        backLog = EditorGUILayout.IntField("Backlog", backLog);
        FPS = EditorGUILayout.IntField("FPS", FPS);
        DPF = EditorGUILayout.IntField("DPF", DPF);
        sendRate = EditorGUILayout.IntField("SendRate", sendRate);
        //------------------------------------------------------------------------------------
        quickPackets = EditorGUILayout.Toggle("Quick Packets", quickPackets);
        noDelay = EditorGUILayout.Toggle("No Delay", noDelay);
        dontDestroyOnLoad = EditorGUILayout.Toggle("Dont Destroy On Load", dontDestroyOnLoad);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        antiCheat = EditorGUILayout.BeginToggleGroup("Anti-Cheat", antiCheat);
        speedHackTolerance = EditorGUILayout.IntField("Speedhack Tolerance", speedHackTolerance);
        teleportTolerance = EditorGUILayout.IntField("Teleport Tolerance", teleportTolerance);
        EditorGUILayout.EndToggleGroup();
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Database Settings", styleText);
        loginUri = EditorGUILayout.TextField("URI Login", loginUri);
        EditorGUILayout.LabelField("Others Settings", styleText);
        EditorGUILayout.LabelField("-", "-");
        EditorGUILayout.LabelField("-", "-");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (GUILayout.Button("Save"))
        {
            SaveJsonSettings();
        }
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void OnFocus()
    {
        LoadSettings();
    }

    void LoadSettings()
    {
        try
        {
            TextAsset fromJson = Resources.Load<TextAsset>("neutronsettings");
            //------------------------------------------------------------------
            JsonUtility.FromJsonOverwrite(fromJson.text, this);
        }
        catch { }
    }

    void SaveJsonSettings()
    {
        string toJson = JsonUtility.ToJson(this);
        //------------------------------------------------------------------------------------
        File.WriteAllText(Application.dataPath + Communication.PATH_SETTINGS, toJson);
        //------------------------------------------------------------------------------------
        AssetDatabase.Refresh();
    }

    void FindViewers()
    {
        if (!onRPCViewerLoaded)
        {
            duplicateEntrys.Clear();
            viewers.Clear();
            onRPCViewerLoaded = true;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                var types = asm.GetTypes();
                foreach (var asmType in types)
                {
                    if (asmType.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        if (asmType.IsSubclassOf(typeof(NeutronBehaviour)) || asmType.IsSubclassOf(typeof(NeutronStatic)))
                        {
                            var mIs = asmType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<RPC>() != null || x.GetCustomAttribute<APC>() != null || x.GetCustomAttribute<RCC>() != null || x.GetCustomAttribute<ACC>() != null).ToArray();
                            if (!viewers.Contains(mIs)) viewers.Add(mIs);
                        }
                    }
                }
            }
        }
    }
}
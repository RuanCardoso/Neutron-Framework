using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Cipher;
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
    [SerializeField] int serverPort = 5055, voicePort = 5056, backLog = 10, serverFPS = 45, serverDPF = 30, serverSendRate = 3, serverSendRateUDP = 3;
    [SerializeField] int serverReceiveRate = 3, serverReceiveRateUDP = 3, clientReceiveRate = 3, clientReceiveRateUDP = 3, clientFPS = 45, clientDPF = 30, clientSendRate = 3, clientSendRateUDP = 3;
    [SerializeField] bool serverNoDelay, clientNoDelay, antiCheat = true, dontDestroyOnLoad = true, UDPDontFragment = true;
    [SerializeField] int speedHackTolerance = 10, teleportTolerance = 15, max_rec_msg, max_send_msg, limit_of_conn_by_ip;
    [SerializeField] string ipAddress = "localhost";

    private Vector2 scroll, scrollEditor;

    [MenuItem("Neutron/Neutron Settings")]
    static void Config()
    {
        var editorAsm = typeof(Editor).Assembly;
        var inspWndType = editorAsm.GetType("UnityEditor.InspectorWindow");
        var editor = GetWindow<NeutronEditor>("Neutron Overview", true, inspWndType);

        //editor.maxSize = new Vector2(10, 10);
        editor.minSize = new Vector2(300, 300);
    }

    private void OnGUI()
    {
        //maxSize = new Vector2(10, 10);
        minSize = new Vector2(300, 300);

        GUIStyle styleText = new GUIStyle(GUI.skin.textArea);
        styleText.fontStyle = FontStyle.Bold;
        styleText.alignment = TextAnchor.MiddleLeft;
        styleText.richText = true;
        styleText.wordWrap = true;

        GUIStyle skinToolbar = ((GUISkin)Resources.Load("Skin/Toolbar", typeof(GUISkin))).GetStyle("TextField");
        windowSelected = GUILayout.Toolbar(windowSelected, new string[] { "Settings", "Permissions", "Calls" }, skinToolbar);
        switch (windowSelected)
        {
            case 0:
                onRPCViewerLoaded = false;
                OnOverview();
                break;
            case 2:
                if (GUILayout.Button("Force Refresh"))
                {
                    onRPCViewerLoaded = false;
                    FindViewers();
                }
                FindViewers();
                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < viewers.Count; i++)
                {
                    duplicateEntrys.Clear();
                    foreach (var mI in viewers[i])
                    {
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

    bool fodoultServerAndClientSettings = true;
    bool fodoultServerSettings = false;
    bool fodoultClientSettings = false;
    bool fodoultServerConstants = false;

    void OnOverview()
    {
        GUIStyle intFieldStyle = new GUIStyle(GUI.skin.label);
        intFieldStyle.fontStyle = FontStyle.BoldAndItalic;
        intFieldStyle.fontSize = 10;

        scrollEditor = EditorGUILayout.BeginScrollView(scrollEditor);
        EditorGUI.BeginChangeCheck();
        fodoultServerAndClientSettings = EditorGUILayout.BeginFoldoutHeaderGroup(fodoultServerAndClientSettings, "[Server & Client Settings]");
        if (fodoultServerAndClientSettings)
        {
            compressionOptions = (Compression)EditorGUILayout.EnumPopup("Compression Mode", compressionOptions);
            ipAddress = EditorGUILayout.TextField("Address", ipAddress);
            serverPort = EditorGUILayout.IntField("Port", serverPort);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        fodoultServerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(fodoultServerSettings, "[Server Settings]");
        if (fodoultServerSettings)
        {
            backLog = EditorGUILayout.IntField("Backlog", backLog);
            serverFPS = EditorGUILayout.IntField("FPS", serverFPS);
            serverDPF = EditorGUILayout.IntField("DPF", serverDPF);
            serverSendRate = EditorGUILayout.IntField("Send Rate(TCP)", serverSendRate);
            serverReceiveRate = EditorGUILayout.IntField("Receive Rate(TCP)", serverReceiveRate);
            serverSendRateUDP = EditorGUILayout.IntField("Send Rate(UDP)", serverSendRateUDP);
            serverReceiveRateUDP = EditorGUILayout.IntField("Receive Rate(UDP)", serverReceiveRateUDP);
            serverNoDelay = EditorGUILayout.Toggle("No Delay", serverNoDelay);
            dontDestroyOnLoad = EditorGUILayout.Toggle("Dont Destroy On Load", dontDestroyOnLoad);

            fodoultServerConstants = EditorGUILayout.Foldout(fodoultServerConstants, "Server Constants");
            if (fodoultServerConstants)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                antiCheat = EditorGUILayout.ToggleLeft("Anti-Cheat", antiCheat);
                EditorGUILayout.EndHorizontal();
                if (antiCheat)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("SPEED_TOLERANCE", intFieldStyle, intFieldStyle);
                    speedHackTolerance = EditorGUILayout.IntField(string.Empty, speedHackTolerance);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("TELE_DIS_TOLERANCE", intFieldStyle, intFieldStyle);
                    teleportTolerance = EditorGUILayout.IntField(string.Empty, teleportTolerance);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("MAX_REC_MSG_SIZE", intFieldStyle, intFieldStyle);
                max_rec_msg = EditorGUILayout.IntField(string.Empty, max_rec_msg);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("MAX_SEND_MSG_SIZE", intFieldStyle, intFieldStyle);
                max_send_msg = EditorGUILayout.IntField(string.Empty, max_send_msg);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("LIMIT_OF_CONN_BY_IP", intFieldStyle, intFieldStyle);
                limit_of_conn_by_ip = EditorGUILayout.IntField(string.Empty, limit_of_conn_by_ip);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        fodoultClientSettings = EditorGUILayout.BeginFoldoutHeaderGroup(fodoultClientSettings, "[Client Settings]");
        if (fodoultClientSettings)
        {
            clientFPS = EditorGUILayout.IntField("FPS", clientFPS);
            clientDPF = EditorGUILayout.IntField("DPF", clientDPF);
            clientSendRate = EditorGUILayout.IntField("Send Rate(TCP)", clientSendRate);
            clientReceiveRate = EditorGUILayout.IntField("Receive Rate(TCP)", clientReceiveRate);
            clientSendRateUDP = EditorGUILayout.IntField("Send Rate(UDP)", clientSendRateUDP);
            clientReceiveRateUDP = EditorGUILayout.IntField("Receive Rate(UDP)", clientReceiveRateUDP);
            clientNoDelay = EditorGUILayout.Toggle("No Delay", clientNoDelay);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        if (EditorGUI.EndChangeCheck()) SaveSettings();
        EditorGUILayout.EndScrollView();
    }

    void LoadSettings() => JsonUtility.FromJsonOverwrite(Resources.Load<TextAsset>("neutronsettings").text.Decrypt(Data.PASS), this);

    void SaveSettings()
    {
        File.WriteAllText(Application.dataPath + Communication.PATH_SETTINGS, JsonUtility.ToJson(this).Encrypt(Data.PASS));
        AssetDatabase.Refresh();
        Utils.Logger("Saved!");
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void OnFocus()
    {
        LoadSettings();
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
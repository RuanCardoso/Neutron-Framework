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
using System.Threading.Tasks;

public class NeutronEditor : EditorWindow
{
    private static bool WakeUp = false;
    #region UI
    private GUIStyle styleText;
    #endregion

    #region Variables
    private int wndIndex = 0;
    private Vector2 viewerScroll;
    #endregion

    #region Collections
    private Dictionary<string, object> Entrys = new Dictionary<string, object>();
    #endregion

    #region Reflection
    private MethodInfo[][] Viewers;
    private Assembly[] assemblies;
    #endregion

    [MenuItem("Neutron/Neutron/Settings")]
    private static void LocateSettings()
    {
        UnityEngine.Object asset = Resources.Load("Neutron Settings");
        AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Neutron/Neutron/Viewer")]
    private static void Viewer()
    {
        void SetWindowSize(NeutronEditor wnd) => wnd.minSize = new Vector2(320, 300);
        void SetWindow()
        {
            var editorAsm = typeof(Editor).Assembly;
            var inspWndType = editorAsm.GetType("UnityEditor.InspectorWindow");
            var neutronEditorWnd = GetWindow<NeutronEditor>("Neutron", true, inspWndType);
            SetWindowSize(neutronEditorWnd);
        }
        SetWindow();
    }

    private void Awake()
    {
        GetAssemblies();
    }

    private void OnEnable()
    {
        ResetWakeUp();
    }

    void SetStyle()
    {
        styleText = new GUIStyle(GUI.skin.textArea);
        styleText.fontStyle = FontStyle.Bold;
        styleText.alignment = TextAnchor.MiddleLeft;
        styleText.richText = true;
        styleText.wordWrap = true;
    }

    private void OnGUI()
    {
        SetStyle();
        GUIStyle skinToolbar = ((GUISkin)Resources.Load("Skin/Toolbar", typeof(GUISkin))).GetStyle("TextField");
        wndIndex = GUILayout.Toolbar(wndIndex, new string[] { "Calls" }, skinToolbar, GUILayout.Width(100));
        viewerScroll = EditorGUILayout.BeginScrollView(viewerScroll);
        ViewerUpdate();
        EditorGUILayout.EndScrollView();
    }

    private void ViewerUpdate()
    {
        try
        {
            switch (wndIndex)
            {
                case 0:
                    if (!WakeUp)
                        Viewers = FindViewers();
                    else
                    {
                        ClearEntrys();
                        for (int i = 0; i < Viewers.Length; i++)
                        {
                            foreach (var viewer in Viewers[i])
                            {
                                bool isDuplicated = false;
                                var parametersInfor = viewer.CustomAttributes.ToArray();
                                var attrName = parametersInfor[0].AttributeType.Name;
                                var value = (int)parametersInfor[0].ConstructorArguments.First().Value;
                                string key = $"{value}:{attrName}";
                                if (!Entrys.ContainsKey(key))
                                    Entrys.Add(key, viewer);
                                else isDuplicated = true;
                                string duplicatedName = (isDuplicated) ? " - <color=red>[Duplicated]</color>" : string.Empty;
                                EditorGUILayout.LabelField($"[<color=#c2c2c2>Method</color>]: <color=#f5c118>{viewer.Name}</color>(ID: <color=#f7382a>{value}</color>){duplicatedName}\r\n[Type]: <color=#26c7fc>{attrName}</color>\r\n[<color=#c2c2c2>Class</color>]: <color=#f5c118>{viewer.DeclaringType.Name}</color>", styleText, GUILayout.Height(50));
                            }
                        }
                    }
                    break;
            }
        }
        catch { }
    }

    private void ClearEntrys()
    {
        Entrys.Clear();
    }

    private void GetAssemblies()
    {
        ResetWakeUp();
        assemblies = AppDomain.CurrentDomain.GetAssemblies();
    }

    private void ResetWakeUp()
    {
        WakeUp = false;
    }

    MethodInfo[][] FindViewers()
    {
        if (assemblies == null)
            GetAssemblies();
        List<MethodInfo[]> listOfMis = new List<MethodInfo[]>();
        foreach (var asm in assemblies)
        {
            var types = asm.GetTypes();
            foreach (var asmType in types)
            {
                if (asmType.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    if (asmType.IsSubclassOf(typeof(NeutronBehaviour)) || asmType.IsSubclassOf(typeof(NeutronStaticBehaviour)))
                    {
                        var mIs = asmType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<RPC>() != null || x.GetCustomAttribute<APC>() != null || x.GetCustomAttribute<Static>() != null || x.GetCustomAttribute<Response>() != null).ToArray();
                        if (!listOfMis.Contains(mIs)) listOfMis.Add(mIs);
                    }
                    else continue;
                }
                else continue;
            }
        }
        WakeUp = true;
        return listOfMis.ToArray();
    }
}
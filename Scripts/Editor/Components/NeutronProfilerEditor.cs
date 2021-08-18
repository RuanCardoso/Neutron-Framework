using NeutronNetwork.Editor;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using UnityEditor;
using UnityEngine;

public class NeutronProfilerEditor : EditorWindow
{
    #region Client;
    private int m_ClientBytesOutgoingTCP;
    private int m_ClientBytesIncomingTCP;

    private int m_ClientBytesOutgoingUDP;
    private int m_ClientBytesIncomingUDP;
    #endregion

    #region Server;
    private int m_ServerBytesOutgoingTCP;
    private int m_ServerBytesIncomingTCP;

    private int m_ServerBytesOutgoingUDP;
    private int m_ServerBytesIncomingUDP;
    #endregion

    [MenuItem("Neutron/Settings/Analysis/Profiler &F9")]
    static void Init()
    {
        EditorWindow Window = GetWindow(typeof(NeutronProfilerEditor), true, "Profiler");
        if (Window != null)
        {
            Window.maxSize = new Vector2(480, 220);
            Window.minSize = new Vector2(480, 220);
        }
    }

    private void OnEnable()
    {
        if (NeutronStatistics.OnChangedStatistics == null)
            NeutronStatistics.OnChangedStatistics += OnChanged;
    }

    private void OnDisable()
    {
        if (NeutronStatistics.OnChangedStatistics != null)
            NeutronStatistics.OnChangedStatistics -= OnChanged;
    }

    private void OnGUI()
    {
        #region Windows
        BeginWindows();
        GUILayout.Window(1, new Rect(5, 5, 230, 200), DrawClientStatistics, "Client");
        GUILayout.Window(2, new Rect(245, 5, 230, 200), DrawServerStatistics, "Server");
        EndWindows();
        #endregion
    }

    private void DrawClientStatistics(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {OthersHelper.SizeSuffix(m_ClientBytesIncomingTCP)} | [{OthersHelper.SizeSuffix(m_ClientBytesIncomingTCP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {OthersHelper.SizeSuffix(m_ClientBytesOutgoingTCP)} | [{OthersHelper.SizeSuffix(m_ClientBytesOutgoingTCP, 2, 4)}]");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {OthersHelper.SizeSuffix(m_ClientBytesIncomingUDP)} | [{OthersHelper.SizeSuffix(m_ClientBytesIncomingUDP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {OthersHelper.SizeSuffix(m_ClientBytesOutgoingUDP)} | [{OthersHelper.SizeSuffix(m_ClientBytesOutgoingUDP, 2, 4)}]");
    }

    private void DrawServerStatistics(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {OthersHelper.SizeSuffix(m_ServerBytesIncomingTCP)} | [{OthersHelper.SizeSuffix(m_ServerBytesIncomingTCP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {OthersHelper.SizeSuffix(m_ServerBytesOutgoingTCP)} | [{OthersHelper.SizeSuffix(m_ServerBytesOutgoingTCP, 2, 4)}]");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {OthersHelper.SizeSuffix(m_ServerBytesIncomingUDP)} | [{OthersHelper.SizeSuffix(m_ServerBytesIncomingUDP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {OthersHelper.SizeSuffix(m_ServerBytesOutgoingUDP)} | [{OthersHelper.SizeSuffix(m_ServerBytesOutgoingUDP, 2, 4)}]");
    }

    private void OnChanged(InOutData[] nProfilers)
    {
        nProfilers[0].Get(out int ClientBytesOutgoingTCP, out int ClientBytesIncomingTCP);
        {
            m_ClientBytesIncomingTCP = ClientBytesIncomingTCP;
            m_ClientBytesOutgoingTCP = ClientBytesOutgoingTCP;
        }

        nProfilers[1].Get(out int ClientBytesOutgoingUDP, out int ClientBytesIncomingUDP);
        {
            m_ClientBytesIncomingUDP = ClientBytesIncomingUDP;
            m_ClientBytesOutgoingUDP = ClientBytesOutgoingUDP;
        }

        nProfilers[2].Get(out int ServerBytesOutgoingTCP, out int ServerBytesIncomingTCP);
        {
            m_ServerBytesIncomingTCP = ServerBytesIncomingTCP;
            m_ServerBytesOutgoingTCP = ServerBytesOutgoingTCP;
        }

        nProfilers[3].Get(out int ServerBytesOutgoingUDP, out int ServerBytesIncomingUDP);
        {
            m_ServerBytesIncomingUDP = ServerBytesIncomingUDP;
            m_ServerBytesOutgoingUDP = ServerBytesOutgoingUDP;
        }

        Repaint();
    }
}
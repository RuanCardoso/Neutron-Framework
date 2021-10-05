using NeutronNetwork.Constants;
using NeutronNetwork.Editor;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using UnityEditor;
using UnityEngine;

public class NeutronProfilerEditor : EditorWindow
{
    private GUIStyle _textAreaStyle;

    #region Client
    private int m_ClientBytesOutgoingTCP;
    private int m_ClientBytesIncomingTCP;

    private int m_ClientBytesOutgoingUDP;
    private int m_ClientBytesIncomingUDP;

    private int m_ClientPacketsOutgoingTCP;
    private int m_ClientPacketsIncomingTCP;

    private int m_ClientPacketsOutgoingUDP;
    private int m_ClientPacketsIncomingUDP;
    #endregion

    #region Server
    private int m_ServerBytesOutgoingTCP;
    private int m_ServerBytesIncomingTCP;

    private int m_ServerBytesOutgoingUDP;
    private int m_ServerBytesIncomingUDP;

    private int m_ServerPacketsOutgoingTCP;
    private int m_ServerPacketsIncomingTCP;

    private int m_ServerPacketsOutgoingUDP;
    private int m_ServerPacketsIncomingUDP;
    #endregion

    [MenuItem("Neutron/Settings/Analysis/Profiler &F9")]
    static void Init()
    {
        EditorWindow Window = GetWindow(typeof(NeutronProfilerEditor), true, "Profiler");
        if (Window != null)
        {
            Window.maxSize = new Vector2(640, 430);
            Window.minSize = new Vector2(640, 430);
        }
    }

    private void OnEnable()
    {
        NeutronStatistics.OnChangedStatistics -= OnChanged;
        NeutronStatistics.OnChangedStatistics += OnChanged;
    }

    private void OnDisable() => NeutronStatistics.OnChangedStatistics -= OnChanged;

    private void OnGUI()
    {
        #region Style
        if (_textAreaStyle == null)
        {
            _textAreaStyle = new GUIStyle(GUI.skin.window)
            {
                wordWrap = true,
                richText = true,
                fontSize = 14,
            };
        }
        #endregion

        #region Windows
        BeginWindows();
        Rect window = new Rect(5, 5, 310, 200);
        window = GUILayout.Window(1, window, DrawClientStatistics, "Client");
        window.x += (window.x + window.y) + window.width;
        window = GUILayout.Window(2, window, DrawServerStatistics, "Server");
        EndWindows();
        #endregion

        #region About
        window.x = 5;
        window.y += window.height + 5;
        window.width = (window.width * 2) + 10;
        window.height += 15;
        GUI.TextArea(window, $"<b><i>Bandwidth usage:</i></b>\r\n<i>AutoSync</i> = ({PacketSize.AutoSync}) Bytes + [parameters] + [header]\r\n<i>gRPC</i> = ({PacketSize.gRPC}) Bytes + [parameters] + [header]\r\n<i>iRPC</i> = ({PacketSize.iRPC}) Bytes + [parameters] + [header]" +
        $"\r\n\r\n<b><i>Header:</i></b>\r\n<i>Client-Side</i> = (1 Byte, 2 Bytes or 4 Bytes)\r\n<i>Server-Side</i> = (1 Byte, 2 Bytes or 4 Bytes) + [2 Bytes]" +
        $"\r\n\r\n<b><i>The default header size is short (2 bytes).\r\nI'm working to further reduce bandwidth usage (:.</i></b>", _textAreaStyle);
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

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(m_ClientBytesIncomingTCP)} | [{Helper.SizeSuffix(m_ClientBytesIncomingTCP, 2, 4)}] - Pkt/s: {m_ClientPacketsIncomingTCP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(m_ClientBytesOutgoingTCP)} | [{Helper.SizeSuffix(m_ClientBytesOutgoingTCP, 2, 4)}] - Pkt/s: {m_ClientPacketsOutgoingTCP}");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(m_ClientBytesIncomingUDP)} | [{Helper.SizeSuffix(m_ClientBytesIncomingUDP, 2, 4)}] - Pkt/s: {m_ClientPacketsIncomingUDP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(m_ClientBytesOutgoingUDP)} | [{Helper.SizeSuffix(m_ClientBytesOutgoingUDP, 2, 4)}] - Pkt/s: {m_ClientPacketsOutgoingUDP}");
    }

    private void DrawServerStatistics(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(m_ServerBytesIncomingTCP)} | [{Helper.SizeSuffix(m_ServerBytesIncomingTCP, 2, 4)}] - Pkt/s: {m_ServerPacketsIncomingTCP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(m_ServerBytesOutgoingTCP)} | [{Helper.SizeSuffix(m_ServerBytesOutgoingTCP, 2, 4)}] - Pkt/s: {m_ServerPacketsOutgoingTCP}");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(m_ServerBytesIncomingUDP)} | [{Helper.SizeSuffix(m_ServerBytesIncomingUDP, 2, 4)}] - Pkt/s: {m_ServerPacketsIncomingUDP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(m_ServerBytesOutgoingUDP)} | [{Helper.SizeSuffix(m_ServerBytesOutgoingUDP, 2, 4)}] - Pkt/s: {m_ServerPacketsOutgoingUDP}");
    }

    private void OnChanged(InOutData[] nProfilers)
    {
        nProfilers[0].Get(out int ClientBytesOutgoingTCP, out int ClientBytesIncomingTCP, out int ClientPacketsOutgoingTCP, out int ClientPacketsIncomingTCP);
        {
            m_ClientBytesIncomingTCP = ClientBytesIncomingTCP;
            m_ClientBytesOutgoingTCP = ClientBytesOutgoingTCP;
            m_ClientPacketsIncomingTCP = ClientPacketsIncomingTCP;
            m_ClientPacketsOutgoingTCP = ClientPacketsOutgoingTCP;
        }

        nProfilers[1].Get(out int ClientBytesOutgoingUDP, out int ClientBytesIncomingUDP, out int ClientPacketsOutgoingUDP, out int ClientPacketsIncomingUDP);
        {
            m_ClientBytesIncomingUDP = ClientBytesIncomingUDP;
            m_ClientBytesOutgoingUDP = ClientBytesOutgoingUDP;
            m_ClientPacketsIncomingUDP = ClientPacketsIncomingUDP;
            m_ClientPacketsOutgoingUDP = ClientPacketsOutgoingUDP;
        }

        nProfilers[2].Get(out int ServerBytesOutgoingTCP, out int ServerBytesIncomingTCP, out int ServerPacketsOutgoingTCP, out int ServerPacketsIncomingTCP);
        {
            m_ServerBytesIncomingTCP = ServerBytesIncomingTCP;
            m_ServerBytesOutgoingTCP = ServerBytesOutgoingTCP;
            m_ServerPacketsIncomingTCP = ServerPacketsIncomingTCP;
            m_ServerPacketsOutgoingTCP = ServerPacketsOutgoingTCP;
        }

        nProfilers[3].Get(out int ServerBytesOutgoingUDP, out int ServerBytesIncomingUDP, out int ServerPacketsOutgoingUDP, out int ServerPacketsIncomingUDP);
        {
            m_ServerBytesIncomingUDP = ServerBytesIncomingUDP;
            m_ServerBytesOutgoingUDP = ServerBytesOutgoingUDP;
            m_ServerPacketsIncomingUDP = ServerPacketsIncomingUDP;
            m_ServerPacketsOutgoingUDP = ServerPacketsOutgoingUDP;
        }

        Repaint();
    }
}
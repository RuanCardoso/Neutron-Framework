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
    #endregion

    #region Server
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
            Window.maxSize = new Vector2(480, 435);
            Window.minSize = new Vector2(480, 435);
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
        Rect window = new Rect(5, 5, 230, 200);
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

        EditorGUILayout.LabelField($"Incoming: {Helper.SizeSuffix(m_ClientBytesIncomingTCP)} | [{Helper.SizeSuffix(m_ClientBytesIncomingTCP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {Helper.SizeSuffix(m_ClientBytesOutgoingTCP)} | [{Helper.SizeSuffix(m_ClientBytesOutgoingTCP, 2, 4)}]");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {Helper.SizeSuffix(m_ClientBytesIncomingUDP)} | [{Helper.SizeSuffix(m_ClientBytesIncomingUDP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {Helper.SizeSuffix(m_ClientBytesOutgoingUDP)} | [{Helper.SizeSuffix(m_ClientBytesOutgoingUDP, 2, 4)}]");
    }

    private void DrawServerStatistics(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {Helper.SizeSuffix(m_ServerBytesIncomingTCP)} | [{Helper.SizeSuffix(m_ServerBytesIncomingTCP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {Helper.SizeSuffix(m_ServerBytesOutgoingTCP)} | [{Helper.SizeSuffix(m_ServerBytesOutgoingTCP, 2, 4)}]");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"Incoming: {Helper.SizeSuffix(m_ServerBytesIncomingUDP)} | [{Helper.SizeSuffix(m_ServerBytesIncomingUDP, 2, 4)}]");
        EditorGUILayout.LabelField($"Outgoing: {Helper.SizeSuffix(m_ServerBytesOutgoingUDP)} | [{Helper.SizeSuffix(m_ServerBytesOutgoingUDP, 2, 4)}]");
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
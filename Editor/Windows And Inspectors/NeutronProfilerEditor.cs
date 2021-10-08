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
    private int _clientBytesOutgoingTCP;
    private int _clientBytesIncomingTCP;

    private int _clientBytesOutgoingUDP;
    private int _clientBytesIncomingUDP;

    private int _clientPacketsOutgoingTCP;
    private int _clientPacketsIncomingTCP;

    private int _clientPacketsOutgoingUDP;
    private int _clientPacketsIncomingUDP;
    #endregion

    #region Server
    private int _serverBytesOutgoingTCP;
    private int _serverBytesIncomingTCP;

    private int _serverBytesOutgoingUDP;
    private int _serverBytesIncomingUDP;

    private int _serverPacketsOutgoingTCP;
    private int _serverPacketsIncomingTCP;

    private int _serverPacketsOutgoingUDP;
    private int _serverPacketsIncomingUDP;
    #endregion

    #region Tcp/Udp
    private int _tcpHeaderSize = 40;
    private int _udpHeaderSize = 28;
    #endregion

    private bool _hasProtocolHeaderIncluded = true;

    [MenuItem("Neutron/Settings/Analysis/Profiler &F9")]
    static void Init()
    {
        EditorWindow Window = GetWindow(typeof(NeutronProfilerEditor), true, "Profiler");
        if (Window != null)
        {
            Window.maxSize = new Vector2(640, 515);
            Window.minSize = new Vector2(640, 515);
        }
    }

    private void OnEnable()
    {
        NeutronStatistics.OnChangedStatistics -= OnChanged;
        NeutronStatistics.OnChangedStatistics += OnChanged;
    }

    private void OnDisable() => NeutronStatistics.OnChangedStatistics -= OnChanged;

    Rect toggleRect = new Rect(5, 500, 110, 10);
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
        window.height += 85;
        GUI.TextArea(window, $"<b><i>Bandwidth usage:</i></b>\r\n<i>AutoSync</i> = ({PacketSize.AutoSync}) Bytes + [parameters] + [header's]\r\n<i>gRPC</i> = ({PacketSize.gRPC}) Bytes + [parameters] + [header's]\r\n<i>iRPC</i> = ({PacketSize.iRPC}) Bytes + [parameters] + [header's]" +
        $"\r\n\r\n<b><i>Neutron Header:</i></b>\r\n<i>Client-Side</i> = (1 Byte, 2 Bytes or 4 Bytes) (Tcp Only)\r\n<i>Server-Side</i> = (1 Byte, 2 Bytes or 4 Bytes) (Tcp Only) + [2 Bytes] (Tcp/Udp)" +
        $"\r\n\r\n<b><i>Protocol Header:</i></b>\r\n<i>Udp + [Ipv4]</i> = 28 Bytes\r\n<i>Tcp + [Ipv4]</i> = 40 Bytes" +
        $"\r\n\r\n<b><i>The default header size is short (2 bytes).\r\nI'm working to further reduce bandwidth usage (:.</i></b>", _textAreaStyle);
        #endregion

        _hasProtocolHeaderIncluded = GUI.Toggle(toggleRect, _hasProtocolHeaderIncluded, "Protocol Header");
        _tcpHeaderSize = _hasProtocolHeaderIncluded ? 40 : 0;
        _udpHeaderSize = _hasProtocolHeaderIncluded ? 28 : 0;
    }

    private void DrawClientStatistics(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(_clientBytesIncomingTCP + (_clientBytesIncomingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesIncomingTCP + (_clientBytesIncomingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsIncomingTCP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(_clientBytesOutgoingTCP + (_clientBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesOutgoingTCP + (_clientBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsOutgoingTCP}");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(_clientBytesIncomingUDP + (_clientBytesIncomingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesIncomingUDP + (_clientBytesIncomingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsIncomingUDP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(_clientBytesOutgoingUDP + (_clientBytesOutgoingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesOutgoingUDP + (_clientBytesOutgoingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsOutgoingUDP}");
    }

    private void DrawServerStatistics(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(_serverBytesIncomingTCP + (_serverBytesIncomingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesIncomingTCP + (_serverBytesIncomingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsIncomingTCP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(_serverBytesOutgoingTCP + (_serverBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesOutgoingTCP + (_serverBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsOutgoingTCP}");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion

        EditorGUILayout.LabelField($"In: {Helper.SizeSuffix(_serverBytesIncomingUDP + (_serverBytesIncomingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesIncomingUDP + (_serverBytesIncomingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsIncomingUDP}");
        EditorGUILayout.LabelField($"Out: {Helper.SizeSuffix(_serverBytesOutgoingUDP + (_serverBytesOutgoingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesOutgoingUDP + (_serverBytesOutgoingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsOutgoingUDP}");
    }

    private void OnChanged(InOutData[] nProfilers)
    {
        nProfilers[0].Get(out int ClientBytesOutgoingTCP, out int ClientBytesIncomingTCP, out int ClientPacketsOutgoingTCP, out int ClientPacketsIncomingTCP);
        {
            _clientBytesIncomingTCP = ClientBytesIncomingTCP;
            _clientBytesOutgoingTCP = ClientBytesOutgoingTCP;
            _clientPacketsIncomingTCP = ClientPacketsIncomingTCP;
            _clientPacketsOutgoingTCP = ClientPacketsOutgoingTCP;
        }

        nProfilers[1].Get(out int ClientBytesOutgoingUDP, out int ClientBytesIncomingUDP, out int ClientPacketsOutgoingUDP, out int ClientPacketsIncomingUDP);
        {
            _clientBytesIncomingUDP = ClientBytesIncomingUDP;
            _clientBytesOutgoingUDP = ClientBytesOutgoingUDP;
            _clientPacketsIncomingUDP = ClientPacketsIncomingUDP;
            _clientPacketsOutgoingUDP = ClientPacketsOutgoingUDP;
        }

        nProfilers[2].Get(out int ServerBytesOutgoingTCP, out int ServerBytesIncomingTCP, out int ServerPacketsOutgoingTCP, out int ServerPacketsIncomingTCP);
        {
            _serverBytesIncomingTCP = ServerBytesIncomingTCP;
            _serverBytesOutgoingTCP = ServerBytesOutgoingTCP;
            _serverPacketsIncomingTCP = ServerPacketsIncomingTCP;
            _serverPacketsOutgoingTCP = ServerPacketsOutgoingTCP;
        }

        nProfilers[3].Get(out int ServerBytesOutgoingUDP, out int ServerBytesIncomingUDP, out int ServerPacketsOutgoingUDP, out int ServerPacketsIncomingUDP);
        {
            _serverBytesIncomingUDP = ServerBytesIncomingUDP;
            _serverBytesOutgoingUDP = ServerBytesOutgoingUDP;
            _serverPacketsIncomingUDP = ServerPacketsIncomingUDP;
            _serverPacketsOutgoingUDP = ServerPacketsOutgoingUDP;
        }

        Repaint();
    }
}
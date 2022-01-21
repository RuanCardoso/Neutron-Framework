using MarkupAttributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Editor;
using NeutronNetwork.Helpers;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Server;
using System;
using System.Collections;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public class NeutronStatistics : MarkupBehaviour
    {
        [InfoBox("This can have some impact on the GC.", EInfoBoxType.Warning)]
        [SerializeField] [Box("Statistics Options")] [Tooltip("Enable statistics for Server GUI.")] private bool _GUIStatsOnServer = true;
        [SerializeField] [Tooltip("Enable statistics for Client GUI.")] private bool _GUIStatsOnClient = true;
        [SerializeField] [Tooltip("Enable statistics for Server console.")] private bool _consoleStats = false;
        public static InOutData[] _inOutDatas;

        #region Client
        public static InOutData ClientTCP { get; } = new InOutData();
        public static InOutData ClientUDP { get; } = new InOutData();
        #endregion

        #region Server
        public static InOutData ServerTCP { get; } = new InOutData();
        public static InOutData ServerUDP { get; } = new InOutData();
        #endregion

        #region Events
        public static NeutronEventNoReturn<InOutData[]> OnChangedStatistics;
        #endregion

        #region Client Data On Server
        private int _clientBytesOutgoingTCP;
        private int _clientBytesIncomingTCP;

        private int _clientBytesOutgoingUDP;
        private int _clientBytesIncomingUDP;

        private int _clientPacketsOutgoingTCP;
        private int _clientPacketsIncomingTCP;

        private int _clientPacketsOutgoingUDP;
        private int _clientPacketsIncomingUDP;
        #endregion

        #region Server Data On Server
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

        [SerializeField] [Tooltip("If true, it must sum the protocol header size into the final result.")] private bool _headerProtocol = true;

        private void Awake() => OnChangedStatistics += OnStatistics;
        private void Start()
        {
            _inOutDatas = new[] { ClientTCP, ClientUDP, ServerTCP, ServerUDP };
        }

        private IEnumerator UpdateAndReset()
        {
            while (false)
            {
                yield return new WaitForSeconds(NeutronConstants.ONE_PER_SECOND);
                OnChangedStatistics?.Invoke(_inOutDatas);
                foreach (var l_Profiler in _inOutDatas)
                    l_Profiler.Set();
            }
        }

        private void OnGUI()
        {
            int padding = 1, height = 55, width = 300;
#if !UNITY_SERVER && !UNITY_EDITOR
            if (_GUIStatsOnClient)
            {
                string tcpIn = $"In: {Helper.SizeSuffix(_clientBytesIncomingTCP + (_clientBytesIncomingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesIncomingTCP + (_clientBytesIncomingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsIncomingTCP}";
                string tcpOut = $"Out: {Helper.SizeSuffix(_clientBytesOutgoingTCP + (_clientBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesOutgoingTCP + (_clientBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsOutgoingTCP}";
                GUI.Box(new Rect(padding, Screen.height - height - padding, width, height), $"TCP[Client]\n{tcpIn}\n{tcpOut}");
                string udpIn = $"In: {Helper.SizeSuffix(_clientBytesIncomingUDP + (_clientBytesIncomingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesIncomingUDP + (_clientBytesIncomingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsIncomingUDP}";
                string udpOut = $"Out: {Helper.SizeSuffix(_clientBytesOutgoingUDP + (_clientBytesOutgoingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_clientBytesOutgoingUDP + (_clientBytesOutgoingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_clientPacketsOutgoingUDP}";
                GUI.Box(new Rect(padding, Screen.height - (height * 2) - (5 + padding), width, height), $"UDP[Client]\n{udpIn}\n{udpOut}");
            }
#endif

#if !UNITY_SERVER && !UNITY_EDITOR && UNITY_NEUTRON_LAN
            if (_GUIStatsOnServer && NeutronServer.Initialized)
            {
                string tcpIn = $"In: {Helper.SizeSuffix(_serverBytesIncomingTCP + (_serverBytesIncomingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesIncomingTCP + (_serverBytesIncomingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsIncomingTCP}";
                string tcpOut = $"Out: {Helper.SizeSuffix(_serverBytesOutgoingTCP + (_serverBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesOutgoingTCP + (_serverBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsOutgoingTCP}";
                GUI.Box(new Rect(padding + width + 5, Screen.height - height - padding, width, height), $"TCP[Server]\n{tcpIn}\n{tcpOut}");
                string udpIn = $"In: {Helper.SizeSuffix(_serverBytesIncomingUDP + (_serverBytesIncomingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesIncomingUDP + (_serverBytesIncomingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsIncomingUDP}";
                string udpOut = $"Out: {Helper.SizeSuffix(_serverBytesOutgoingUDP + (_serverBytesOutgoingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesOutgoingUDP + (_serverBytesOutgoingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsOutgoingUDP}";
                GUI.Box(new Rect(padding + width + 5, Screen.height - (height * 2) - (5 + padding), width, height), $"UDP[Server]\n{udpIn}\n{udpOut}");
            }
#endif
        }

        private void UpdateInServerConsole()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (_consoleStats)
            {
            #region Header
                //LogHelper.Info($"\r\nTCP[Server] - {NeutronFramerate.Fps} Fps");
            #endregion

                //LogHelper.Info($"In: {Helper.SizeSuffix(_serverBytesIncomingTCP + (_serverBytesIncomingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesIncomingTCP + (_serverBytesIncomingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsIncomingTCP}");
                //LogHelper.Info($"Out: {Helper.SizeSuffix(_serverBytesOutgoingTCP + (_serverBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesOutgoingTCP + (_serverBytesOutgoingTCP > 0 ? _tcpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsOutgoingTCP}");

                string separator = string.Empty;
                for (int i = 0; i < Console.WindowWidth; i++)
                    separator += "*";
                LogHelper.Info($"\r\n{separator}");

            #region Header
                LogHelper.Info($"UDP[Server] - {NeutronFramerate.Fps} Fps");
            #endregion

                LogHelper.Info($"In: {Helper.SizeSuffix(_serverBytesIncomingUDP + (_serverBytesIncomingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesIncomingUDP + (_serverBytesIncomingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsIncomingUDP}");
                LogHelper.Info($"Out: {Helper.SizeSuffix(_serverBytesOutgoingUDP + (_serverBytesOutgoingUDP > 0 ? _udpHeaderSize : 0))} | [{Helper.SizeSuffix(_serverBytesOutgoingUDP + (_serverBytesOutgoingUDP > 0 ? _udpHeaderSize : 0), 2, 4)}] - Pkt/s: {_serverPacketsOutgoingUDP}");
            }
#endif
        }

        private void OnStatistics(InOutData[] nProfilers)
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

            UpdateInServerConsole();
        }

        private void Update()
        {
            _tcpHeaderSize = _headerProtocol ? 40 : 0;
            _udpHeaderSize = _headerProtocol ? 28 : 0;
        }

        //private void OnApplicationQuit()
        //{
        //    foreach (var l_Profiler in _inOutDatas)
        //        l_Profiler.Set();
        //    OnChangedStatistics?.Invoke(_inOutDatas);
        //}
    }
}
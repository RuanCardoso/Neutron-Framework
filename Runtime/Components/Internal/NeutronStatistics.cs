using NeutronNetwork.Constants;
using NeutronNetwork.Editor;
using NeutronNetwork.Helpers;
using System;
using System.Collections;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public class NeutronStatistics : MonoBehaviour
    {
        [SerializeField]
        private bool _enableProfilerOnServer;
        public static bool EnableProfilerOnServer
        {
            get;
            private set;
        }
        private readonly InOutData[] m_Profilers = new[] { ClientTCP, ClientUDP, ServerTCP, ServerUDP };

        #region Client
        public static InOutData ClientTCP { get; } = new InOutData();
        public static InOutData ClientUDP { get; } = new InOutData();
        #endregion

        #region Server
        public static InOutData ServerTCP { get; } = new InOutData();
        public static InOutData ServerUDP { get; } = new InOutData();
        #endregion

        #region Events
        public static event NeutronEventNoReturn<InOutData[]> OnChangedStatistics;
        #endregion

        #region Client Data On Server
        private int m_ClientBytesOutgoingTCP;
        private int m_ClientBytesIncomingTCP;

        private int m_ClientBytesOutgoingUDP;
        private int m_ClientBytesIncomingUDP;

        private int m_ClientPacketsOutgoingTCP;
        private int m_ClientPacketsIncomingTCP;

        private int m_ClientPacketsOutgoingUDP;
        private int m_ClientPacketsIncomingUDP;
        #endregion

        #region Server Data On Server
        private int m_ServerBytesOutgoingTCP;
        private int m_ServerBytesIncomingTCP;

        private int m_ServerBytesOutgoingUDP;
        private int m_ServerBytesIncomingUDP;

        private int m_ServerPacketsOutgoingTCP;
        private int m_ServerPacketsIncomingTCP;

        private int m_ServerPacketsOutgoingUDP;
        private int m_ServerPacketsIncomingUDP;
        #endregion

#if UNITY_SERVER && !UNITY_EDITOR
        private void Awake() => OnChangedStatistics += OnStatistics;
#endif
        private void Start() => StartCoroutine(UpdateAndReset());

        private IEnumerator UpdateAndReset()
        {
            EnableProfilerOnServer = _enableProfilerOnServer;
            while (true)
            {
                yield return new WaitForSeconds(NeutronConstants.ONE_PER_SECOND);
                OnChangedStatistics?.Invoke(m_Profilers);
                foreach (var l_Profiler in m_Profilers)
                    l_Profiler.Reset();
            }
        }

        private void OnUpdate()
        {
            if (EnableProfilerOnServer)
            {
                //#region Header
                //LogHelper.Info("\r\nTCP[Client]");
                //#endregion

                //LogHelper.Info($"In: {Helper.SizeSuffix(m_ClientBytesIncomingTCP)} | [{Helper.SizeSuffix(m_ClientBytesIncomingTCP, 2, 4)}] - Pkt/s: {m_ClientPacketsIncomingTCP}");
                //LogHelper.Info($"Out: {Helper.SizeSuffix(m_ClientBytesOutgoingTCP)} | [{Helper.SizeSuffix(m_ClientBytesOutgoingTCP, 2, 4)}] - Pkt/s: {m_ClientPacketsOutgoingTCP}");

                //#region Header
                //LogHelper.Info("\r\nUDP[Client]");
                //#endregion

                //LogHelper.Info($"In: {Helper.SizeSuffix(m_ClientBytesIncomingUDP)} | [{Helper.SizeSuffix(m_ClientBytesIncomingUDP, 2, 4)}] - Pkt/s: {m_ClientPacketsIncomingUDP}");
                //LogHelper.Info($"Out: {Helper.SizeSuffix(m_ClientBytesOutgoingUDP)} | [{Helper.SizeSuffix(m_ClientBytesOutgoingUDP, 2, 4)}] - Pkt/s: {m_ClientPacketsOutgoingUDP}");

                #region Header
                LogHelper.Info("\r\nTCP[Server]");
                #endregion

                LogHelper.Info($"In: {Helper.SizeSuffix(m_ServerBytesIncomingTCP)} | [{Helper.SizeSuffix(m_ServerBytesIncomingTCP, 2, 4)}] - Pkt/s: {m_ServerPacketsIncomingTCP}");
                LogHelper.Info($"Out: {Helper.SizeSuffix(m_ServerBytesOutgoingTCP)} | [{Helper.SizeSuffix(m_ServerBytesOutgoingTCP, 2, 4)}] - Pkt/s: {m_ServerPacketsOutgoingTCP}");

                string separator = string.Empty;
                for (int i = 0; i < Console.WindowWidth; i++)
                    separator += "*";
                LogHelper.Info($"\r\n{separator}");

                #region Header
                LogHelper.Info("UDP[Server]");
                #endregion

                LogHelper.Info($"In: {Helper.SizeSuffix(m_ServerBytesIncomingUDP)} | [{Helper.SizeSuffix(m_ServerBytesIncomingUDP, 2, 4)}] - Pkt/s: {m_ServerPacketsIncomingUDP}");
                LogHelper.Info($"Out: {Helper.SizeSuffix(m_ServerBytesOutgoingUDP)} | [{Helper.SizeSuffix(m_ServerBytesOutgoingUDP, 2, 4)}] - Pkt/s: {m_ServerPacketsOutgoingUDP}");
            }
        }

        private void OnStatistics(InOutData[] nProfilers)
        {
            if (EnableProfilerOnServer)
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
                OnUpdate();
            }
        }

        private void OnApplicationQuit()
        {
            foreach (var l_Profiler in m_Profilers)
                l_Profiler.Reset();
            OnChangedStatistics?.Invoke(m_Profilers);
        }
    }
}
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public class NeutronStatistics : MonoBehaviour
    {
        #region Variables
        float t_Timer = 0;
        #endregion

        #region Client;
        public static NeutronStatisticsProfiler m_ClientTCP = new NeutronStatisticsProfiler();
        public static NeutronStatisticsProfiler m_ClientUDP = new NeutronStatisticsProfiler();
        #endregion

        #region Server;
        public static NeutronStatisticsProfiler m_ServerTCP = new NeutronStatisticsProfiler();
        public static NeutronStatisticsProfiler m_ServerUDP = new NeutronStatisticsProfiler();
        #endregion

        private void Start()
        {
#if UNITY_SERVER || UNITY_EDITOR
#endif
        }

        private void Update()
        {
            t_Timer += Time.deltaTime;
            if (t_Timer >= 1)
            {
                m_ClientTCP.Reset();
                m_ClientUDP.Reset();
                m_ServerTCP.Reset();
                m_ServerUDP.Reset();
                t_Timer = 0;
            }
        }
    }
}
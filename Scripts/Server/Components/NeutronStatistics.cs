using NeutronNetwork.Constants;
using System.Collections;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public class NeutronStatistics : MonoBehaviour
    {
        #region Client;
        public static InOutData m_ClientTCP = new InOutData();
        public static InOutData m_ClientUDP = new InOutData();
        #endregion

        #region Server;
        public static InOutData m_ServerTCP = new InOutData();
        public static InOutData m_ServerUDP = new InOutData();
        #endregion

        #region Events
        public static NeutronEventNoReturn<InOutData[]> OnChangedStatistics;
        #endregion

        private InOutData[] m_Profilers = new[] { m_ClientTCP, m_ClientUDP, m_ServerTCP, m_ServerUDP };

        private void Start()
        {
            StartCoroutine(Clear());
        }

        private IEnumerator Clear()
        {
            while (true)
            {
                yield return new WaitForSeconds(NeutronConstantsSettings.ONE_PER_SECOND);
                OnChangedStatistics?.Invoke(m_Profilers);
                #region Reset
                foreach (var l_Profiler in m_Profilers)
                    l_Profiler.Reset();
                #endregion
            }
        }
    }
}
using System.Collections;
using NeutronNetwork.Constants;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public class NeutronStatistics : MonoBehaviour
    {
        #region Client;
        public static NSP m_ClientTCP = new NSP();
        public static NSP m_ClientUDP = new NSP();
        #endregion

        #region Server;
        public static NSP m_ServerTCP = new NSP();
        public static NSP m_ServerUDP = new NSP();
        #endregion

        #region Events
        public static NeutronEventNoReturn<NSP[]> OnChangedStatistics = new NeutronEventNoReturn<NSP[]>();
        #endregion

        private NSP[] m_Profilers = new[] { m_ClientTCP, m_ClientUDP, m_ServerTCP, m_ServerUDP };

        private void Start()
        {
            StartCoroutine(Clear());
        }

        private IEnumerator Clear()
        {
            while (true)
            {
                yield return new WaitForSeconds(NeutronConstants.ONE_PER_SECOND);
                OnChangedStatistics.Invoke(m_Profilers);
                #region Reset
                foreach (var l_Profiler in m_Profilers)
                    l_Profiler.Reset();
                #endregion
            }
        }
    }
}
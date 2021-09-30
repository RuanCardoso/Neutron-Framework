using NeutronNetwork.Constants;
using NeutronNetwork.Editor;
using System.Collections;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public class NeutronStatistics : MonoBehaviour
    {
        #region Client
        public static InOutData ClientTCP { get; set; } = new InOutData();
        public static InOutData ClientUDP { get; set; } = new InOutData();
        #endregion

        #region Server
        public static InOutData ServerTCP { get; set; } = new InOutData();
        public static InOutData ServerUDP { get; set; } = new InOutData();
        #endregion

        #region Events
        public static NeutronEventNoReturn<InOutData[]> OnChangedStatistics;
        #endregion

        private readonly InOutData[] m_Profilers = new[] { ClientTCP, ClientUDP, ServerTCP, ServerUDP };

        private void Start() => StartCoroutine(Clear());

        private IEnumerator Clear()
        {
            while (true)
            {
                yield return new WaitForSeconds(NeutronConstantsSettings.ONE_PER_SECOND);
                //************************************************************************
                OnChangedStatistics?.Invoke(m_Profilers);
                //************************************************************************
                foreach (var l_Profiler in m_Profilers)
                    l_Profiler.Reset();
            }
        }
    }
}
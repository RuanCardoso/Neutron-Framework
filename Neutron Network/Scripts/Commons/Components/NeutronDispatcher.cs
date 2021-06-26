using System;
using System.Reflection;
using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Wrappers;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_DISPATCHER)]
    public class NeutronDispatcher : MonoBehaviour
    {
        #region Variables
        private int m_ChunkSize;
        #endregion

        #region Collections
        private static NeutronSafeQueue<Action> m_ActionsDispatcher = new NeutronSafeQueue<Action>();
        #endregion

        private void Awake()
        {
#if UNITY_EDITOR
            m_ChunkSize = NeutronConfig.Settings.EditorSettings.DispatcherChunkSize * 2;
#elif UNITY_SERVER
            m_ChunkSize = NeutronConfig.Settings.ServerSettings.DispatcherChunkSize;
#else
            m_ChunkSize = NeutronConfig.Settings.ClientSettings.DispatcherChunkSize;
#endif
        }

        [ThreadSafe]
        private void Update()
        {
            try
            {
                for (int i = 0; i < m_ChunkSize && m_ActionsDispatcher.Count > 0; i++)
                {
                    if (m_ActionsDispatcher.TryDequeue(out Action action))
                        action.Invoke();
                    else { }
                }
            }
            catch (Exception ex) { StackTrace(ex); }
        }

        [ThreadSafe]
        public static void Dispatch(Action action)
        {
            m_ActionsDispatcher.Enqueue(action);
        }

        void StackTrace(Exception ex)
        {
            NeutronLogger.StackTrace(ex);
            NeutronLogger.StackTrace(ex.InnerException);
        }
    }
}
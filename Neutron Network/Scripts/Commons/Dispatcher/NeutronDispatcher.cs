using System;
using System.Reflection;
using NeutronNetwork;
using NeutronNetwork.Internal.Wrappers;
using UnityEngine;

[DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_DISPATCHER)]
public class NeutronDispatcher : MonoBehaviour
{
    #region Variables
    private int m_ChunkSize;
    #endregion

    #region Collections
    public static NeutronQueue<Action> m_ActionsDispatcher = new NeutronQueue<Action>();
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

    private void Update()
    {
        try
        {
            for (int i = 0; i < m_ChunkSize && m_ActionsDispatcher.SafeCount > 0; i++)
                m_ActionsDispatcher.SafeDequeue().Invoke();
        }
        catch (Exception ex) { StackTrace(ex); }
    }

    void StackTrace(Exception ex)
    {
        NeutronUtils.StackTrace(ex);
        NeutronUtils.StackTrace(ex.InnerException);
    }
}
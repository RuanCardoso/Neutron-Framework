using System;
using NeutronNetwork;
using NeutronNetwork.Internal.Wrappers;
using UnityEngine;

[DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_DISPATCHER_ORDER)]
public class NeutronDispatcher : MonoBehaviour
{
    private int m_ChunkSize;
    public static NeutronQueue<Action> m_ActionsDispatcher = new NeutronQueue<Action>();

    private void Awake()
    {
#if UNITY_EDITOR
        m_ChunkSize = NeutronConfig.Settings.EditorSettings.DispatcherChunkSize;
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
            {
                m_ActionsDispatcher.SafeDequeue().Invoke();
            }
        }
        catch (Exception ex) { NeutronUtils.StackTrace(ex); }
    }
}
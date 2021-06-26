using NeutronNetwork.Constants;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_CONFIG)]
    public class NeutronConfig : MonoBehaviour
    {
        public static NeutronSettings Settings { get; private set; }

        #region Variables
        private int m_Framerate;
        #endregion

        private void Awake()
        {
            LoadSettings();
        }

        private void Start()
        {
            DontDestroyOnLoad(transform.root);

            #region Framerate
#if UNITY_EDITOR
            m_Framerate = NeutronConfig.Settings.EditorSettings.FPS;
#elif UNITY_SERVER
            m_Framerate = NeutronConfig.Settings.ServerSettings.FPS;
#else
            m_Framerate = NeutronConfig.Settings.ClientSettings.FPS;
#endif
            if (m_Framerate > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = m_Framerate;
            }
            #endregion
        }

        private void LoadSettings()
        {
            if (Settings == null)
                Settings = Resources.Load<NeutronSettings>("Neutron Settings");
        }
    }
}
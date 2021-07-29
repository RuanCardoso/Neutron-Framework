using NeutronNetwork.Constants;
using System.Diagnostics;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONFIG)]
    public class NeutronMain : MonoBehaviour
    {
        #region Properties
        public static Settings Settings { get; set; }
        public static Synchronization Synchronization { get; set; }
        #endregion

        #region Fields
        public static Stopwatch Chronometer = new Stopwatch();
        private int _framerate;
        #endregion

        private void Awake()
        {
#if UNITY_SERVER
            Chronometer.Start();
#endif
            LoadSettings();
            LoadSynchronization();
        }

        private void OnEnable()
        {
            DontDestroyOnLoad(transform.root);
        }

        private void Start()
        {
            Framerate();
        }

        private void Update()
        {
            Neutron.Time = Chronometer.Elapsed.TotalSeconds + Neutron.DiffTime;
        }

        private void Framerate()
        {
#if UNITY_EDITOR
            _framerate = NeutronMain.Settings.EditorSettings.FPS;
#elif UNITY_SERVER
            _framerate = NeutronMain.Settings.ServerSettings.FPS;
#else
            _framerate = NeutronMain.Settings.ClientSettings.FPS;
#endif
            if (_framerate > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = _framerate;
            }
        }

        private void LoadSettings()
        {
            if (Settings == null)
            {
                Settings = Resources.Load<Settings>("Neutron Settings");
                if (Settings == null)
                {
                    if (!LogHelper.Error("Settings missing!"))
                        Destroy(gameObject);
                }
            }
        }

        public void LoadSynchronization()
        {
            if (Synchronization == null)
            {
                Synchronization = Resources.Load<Synchronization>("Neutron Synchronization");
                if (Synchronization == null)
                {
                    if (!LogHelper.Error("Synchronization missing!"))
                        Destroy(gameObject);
                }
            }
        }

        public static Synchronization EditorLoadSynchronization()
        {
            return Resources.Load<Synchronization>("Neutron Synchronization");
        }
    }
}
using NeutronNetwork.Constants;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using static NeutronNetwork.Extensions.CipherExt;

namespace NeutronNetwork.Internal.Components
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONFIG)]
    public class NeutronModule : MonoBehaviour
    {
        public static Stopwatch Chronometer = new Stopwatch();

        #region Properties
        public static Settings Settings { get; set; }
        public static Synchronization Synchronization { get; set; }
        public static Encoding Encoding { get; set; }
        public static int HeaderSize { get; set; }
        #endregion

        #region Fields
        private int _framerate;
        #endregion

        private void Awake()
        {
#if UNITY_SERVER
            Chronometer.Start();
#endif
            LoadSettings();
            LoadSynchronization();
            InitializePools();
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
#if UNITY_EDITOR
            if (Settings.GlobalSettings.PerfomanceMode)
                UnityEditor.Selection.activeGameObject = null;
#endif
        }

        private void Framerate()
        {
#if UNITY_EDITOR
            _framerate = NeutronModule.Settings.EditorSettings.FPS;
#elif UNITY_SERVER
            _framerate = NeutronModule.Settings.ServerSettings.FPS;
#else
            _framerate = NeutronModule.Settings.ClientSettings.FPS;
#endif
            if (_framerate > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = _framerate;
            }
        }

        private void InitializePools()
        {
            int maxCapacity = Settings.GlobalSettings.PoolCapacity;
            Neutron.PooledNetworkWriters = new NeutronPool<NeutronWriter>(() => new NeutronWriter(), maxCapacity, false);
            Neutron.PooledNetworkReaders = new NeutronPool<NeutronReader>(() => new NeutronReader(), maxCapacity, false);
            Neutron.PooledNetworkStreams = new NeutronPool<NeutronStream>(() => new NeutronStream(true), maxCapacity, false);
            for (int i = 0; i < maxCapacity; i++)
            {
                Neutron.PooledNetworkWriters.Push(new NeutronWriter());
                Neutron.PooledNetworkReaders.Push(new NeutronReader());
                Neutron.PooledNetworkStreams.Push(new NeutronStream(true));
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
                else
                {
                    switch (Settings.NetworkSettings.Encoding)
                    {
                        case EncodingType.ASCII:
                            Encoding = Encoding.ASCII;
                            break;
                        case EncodingType.BigEndianUnicode:
                            Encoding = Encoding.BigEndianUnicode;
                            break;
                        case EncodingType.Default:
                            Encoding = Encoding.Default;
                            break;
                        case EncodingType.Unicode:
                            Encoding = Encoding.Unicode;
                            break;
                        case EncodingType.UTF32:
                            Encoding = Encoding.UTF32;
                            break;
                        case EncodingType.UTF7:
                            Encoding = Encoding.UTF7;
                            break;
                        case EncodingType.UTF8:
                            Encoding = Encoding.UTF8;
                            break;
                    }

                    switch (Settings.NetworkSettings.HeaderSize)
                    {
                        case HeaderSizeType.Byte:
                            HeaderSize = sizeof(byte);
                            break;
                        case HeaderSizeType.Short:
                            HeaderSize = sizeof(short);
                            break;
                        case HeaderSizeType.Int:
                            HeaderSize = sizeof(int);
                            break;
                    }
                }
            }
        }

        private void LoadSynchronization()
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

#if UNITY_EDITOR
        public static Settings EditorLoadSettings()
        {
            var settings = Resources.Load<Settings>("Neutron Settings");
            if (settings == null)
                LogHelper.Error("Settings missing!");
            return settings;
        }
#endif

        public static void SetPassword(string password)
        {
            PassPhrase = password;
        }
    }
}
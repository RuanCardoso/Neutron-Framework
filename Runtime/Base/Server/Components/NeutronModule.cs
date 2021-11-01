using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using static NeutronNetwork.Extensions.CipherExt;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONFIG)]
    public class NeutronModule : MonoBehaviour
    {
        public NeutronModule() { }

        #region Properties
        public static Settings Settings
        {
            get;
            private set;
        }

        public static Synchronization Synchronization
        {
            get;
            private set;
        }

        public static Encoding Encoding
        {
            get;
            private set;
        }

        public static int HeaderSize
        {
            get;
            private set;
        }

        public static int UnityThreadId
        {
            get;
            private set;
        }
        #endregion

        #region Properties -> Events
        public static NeutronEventNoReturn<Settings> OnLoadSettings
        {
            get;
            set;
        }
        #endregion

        #region Fields
        private int _framerate;
#pragma warning disable IDE0044
        [SerializeField] private bool _autoSimulation = false;
#pragma warning restore IDE0044
        #endregion

        private void Awake()
        {
            UnityThreadId = ThreadHelper.GetThreadID();
            LoadSettings();
            LoadSynchronizationSettings();
            InitializePools();
        }

        [Obsolete]
        private void Start()
        {
            SetFramerate();
            //* A física não deve ser auto-simulada, neutron usa física separada por cena, e as simula manualmente.
            Physics.autoSimulation = _autoSimulation;
#if UNITY_2020_1_OR_NEWER
            Physics2D.simulationMode = !_autoSimulation ? SimulationMode2D.Script : SimulationMode2D.FixedUpdate;
#else
            Physics2D.autoSimulation = _autoSimulation;
#endif
#if UNITY_SERVER && !UNITY_EDITOR
            Debug.unityLogger.logEnabled = false;
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Settings.GlobalSettings.PerfomanceMode)
                UnityEditor.Selection.activeGameObject = null;
#endif
        }

        private void OnEnable()
        {
            DontDestroyOnLoad(transform.root);
        }

        private void SetFramerate()
        {
            _framerate = Settings.GlobalSettings.FPS;
            if (_framerate > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = _framerate;
            }
        }

        private void InitializePools()
        {
            Neutron.PooledNetworkStreams = new NeutronPool<NeutronStream>(() => new NeutronStream(true), Settings.GlobalSettings.StreamPoolCapacity, false, "Neutron Streams");
            Neutron.PooledNetworkPackets = new NeutronPool<NeutronPacket>(() => new NeutronPacket(), Settings.GlobalSettings.PacketPoolCapacity, false, "Neutron Packets");
            for (int i = 0; i < Settings.GlobalSettings.StreamPoolCapacity; i++)
                Neutron.PooledNetworkStreams.Push(new NeutronStream(true));
            for (int i = 0; i < Settings.GlobalSettings.PacketPoolCapacity; i++)
                Neutron.PooledNetworkPackets.Push(new NeutronPacket());
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
                    OnLoadSettings?.Invoke(Settings);
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
                    StateObject.Size = Helper.GetConstants().Udp.MaxUdpPacketSize;
                }
            }
        }

        private void LoadSynchronizationSettings()
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

        public static void SetPassword(string password) => PassPhrase = password;
    }
}
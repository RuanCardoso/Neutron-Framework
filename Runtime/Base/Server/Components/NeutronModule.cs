using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using System;
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

        public static bool IsUnityThread
        {
            get;
            private set;
        }

        public static GameObject ClientObject
        {
            get;
            private set;
        }

        public static GameObject ServerObject
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
#pragma warning disable IDE0044
        [SerializeField] private bool _autoSimulation = false;
#pragma warning restore IDE0044
        #endregion

#pragma warning disable IDE0051
        private void Awake()
#pragma warning restore IDE0051
        {
            UnityThreadId = ThreadHelper.GetThreadID();
            LoadSettings();
            LoadSynchronizationSettings();
            InitializePools();
        }

        [Obsolete]
#pragma warning disable IDE0051
        private void Start()
#pragma warning restore IDE0051
        {
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
            GameObject controllers = GameObject.Find("Controllers");
            ClientObject = controllers.transform.Find("Client").gameObject;
            ServerObject = controllers.transform.Find("Server").gameObject;
        }

#pragma warning disable IDE0051
        private void OnEnable() => DontDestroyOnLoad(transform.root);
#pragma warning restore IDE0051

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
                        Destroy(transform.root);
                }
                else
                {
                    OnLoadSettings?.Invoke(Settings);
                    IsUnityThread = Settings.GlobalSettings.Performance == ThreadType.Unity;

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

        public static void SetPassword(string password) => PassPhrase = password;
    }
}
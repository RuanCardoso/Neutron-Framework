using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
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
            SetRateFrequency();
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

        private void OnEnable() => DontDestroyOnLoad(transform.root);

        private void InitializePools()
        {
            Neutron.PooledNetworkStreams = new NeutronPool<NeutronStream>(() => new NeutronStream(true), Settings.GlobalSettings.StreamPoolCapacity, false, "Neutron Streams");
            Neutron.PooledNetworkPackets = new NeutronPool<NeutronPacket>(() => new NeutronPacket(), Settings.GlobalSettings.PacketPoolCapacity, false, "Neutron Packets");
            for (int i = 0; i < Settings.GlobalSettings.StreamPoolCapacity; i++)
                Neutron.PooledNetworkStreams.Push(new NeutronStream(true));
            for (int i = 0; i < Settings.GlobalSettings.PacketPoolCapacity; i++)
                Neutron.PooledNetworkPackets.Push(new NeutronPacket());
        }

        private void SetRateFrequency()
        {
            QualitySettings.vSyncCount = 0;
            currentFrameTime = Time.realtimeSinceStartup;
            StartCoroutine(WaitForNextFrame(Settings.GlobalSettings.Fps));
        }

        private float currentFrameTime;
        private IEnumerator WaitForNextFrame(float rate)
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                currentFrameTime += 1.0f / rate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = currentFrameTime - t - 0.01f;
                if (sleepTime > 0)
                    Thread.Sleep((int)(sleepTime * 1000));
                while (t < currentFrameTime)
                    t = Time.realtimeSinceStartup;
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

        public static void SetPassword(string password) => PassPhrase = password;
    }
}
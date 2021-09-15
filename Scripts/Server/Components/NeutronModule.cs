using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using static NeutronNetwork.Extensions.CipherExt;

namespace NeutronNetwork
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

        #region Properties -> Events
        public static NeutronEventNoReturn OnUpdate { get; set; }
        public static NeutronEventNoReturn OnFixedUpdate { get; set; }
        public static NeutronEventNoReturn OnLateUpdate { get; set; }
        public static NeutronEventNoReturn<Settings> OnLoadSettings { get; set; }
        #endregion

        #region Fields
        private int _framerate;
        #endregion

        private void Awake()
        {
#if UNITY_EDITOR
            CreateLogDelegate();
#endif
#if UNITY_SERVER || UNITY_NEUTRON_LAN
            Chronometer.Start();
#endif
            LoadSettings();
            LoadSynchronization();
            InitializePools();
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
            OnUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        private void OnEnable()
        {
            DontDestroyOnLoad(transform.root);
        }

        private void Framerate()
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
            int maxCapacity = Settings.GlobalSettings.StreamPoolCapacity;
            int maxCapacityPackets = Settings.GlobalSettings.PacketPoolCapacity;
            Neutron.PooledNetworkWriters = new NeutronPool<NeutronWriter>(() => new NeutronWriter(), maxCapacity, false, "Neutron Writers");
            Neutron.PooledNetworkReaders = new NeutronPool<NeutronReader>(() => new NeutronReader(), maxCapacity, false, "Neutron Readers");
            Neutron.PooledNetworkStreams = new NeutronPool<NeutronStream>(() => new NeutronStream(true), maxCapacity, false, "Neutron Streams");
            Neutron.PooledNetworkPackets = new NeutronPool<NeutronPacket>(() => new NeutronPacket(), maxCapacityPackets, false, "Neutron Packets");
            for (int i = 0; i < maxCapacity; i++)
            {
                Neutron.PooledNetworkWriters.Push(new NeutronWriter());
                Neutron.PooledNetworkReaders.Push(new NeutronReader());
                Neutron.PooledNetworkStreams.Push(new NeutronStream(true));
            }

            for (int i = 0; i < maxCapacityPackets; i++)
            {
                Neutron.PooledNetworkPackets.Push(new NeutronPacket());
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
                    StateObject.Size = OthersHelper.GetConstants().MaxUdpPacketSize;
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

        #region Log
        private void CreateLogDelegate()
        {
            LogHelper.LogErrorWithoutStackTrace = (Action<string, string, int, int>)typeof(UnityEngine.Debug).GetMethod(
                "LogPlayerBuildError",
                BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.NonPublic
            ).CreateDelegate(typeof(Action<string, string, int, int>), null);
        }
        #endregion
    }
}
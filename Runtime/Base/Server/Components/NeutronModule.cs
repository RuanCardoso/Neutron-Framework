using MarkupAttributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using System;
using System.Net.Sockets;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static NeutronNetwork.Extensions.CipherExt;
using ANaughty = NeutronNetwork.Naughty.Attributes;

namespace NeutronNetwork
{
    /// <summary>
    /// This class is responsible to load and initialize the settings.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONFIG)]
    public class NeutronModule : MarkupBehaviour
    {
        private enum BuildMode { Debug, Release, Master }
        #region Properties
        /// <summary>
        /// Settings of the Neutron.
        /// </summary>
        /// <value></value>
        public static StateSettings Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// ............
        /// </summary>
        /// <value></value>
        public static Synchronization Synchronization
        {
            get;
            private set;
        }

        /// <summary>
        /// The encoding used to convert the bytes to strings or vice versa.
        /// </summary>
        /// <value></value>
        public static Encoding Encoding
        {
            get;
            private set;
        }

        /// <summary>
        /// The maximum stack allocation size. If the size is greater than that, ArrayPool will be used, otherwise stackalloc will be used.<br/>
        /// Possible stackoverflow when the size is too large.
        /// </summary>
        /// <value></value>
        public static int MaxStackAllocSize
        {
            get;
            private set;
        }

        /// <summary>
        /// This can be Byte(1) or Short(2) or Int(4), used to write the length of the packet.
        /// </summary>
        /// <value></value>
        public static int HeaderSize
        {
            get;
            private set;
        }

        /// <summary>
        /// The Unity thread id, used to check if the current thread is the Unity thread or Neutron thread.
        /// </summary>
        /// <value></value>
        public static int UnityThreadId
        {
            get;
            private set;
        }

        /// <summary>
        /// Used to check if the current thread is the Unity thread or Neutron thread.
        /// </summary>
        /// <value></value>
        public static bool IsUnityThread
        {
            get;
            private set;
        }

        /// <summary>
        /// Client side object in hierarchy.
        /// </summary>
        /// <value></value>
        public static GameObject ClientObject
        {
            get;
            private set;
        }

        /// <summary>
        /// Server side object in hierarchy.
        /// </summary>
        /// <value></value>
        public static GameObject ServerObject
        {
            get;
            private set;
        }
        #endregion

        #region Properties -> Events
        /// <summary>
        /// Used to set the settings of the Neutron before the initialization.
        /// </summary>
        /// <value></value>
        public static NeutronEventNoReturn<StateSettings> OnLoadSettings
        {
            get;
            set;
        }
        #endregion

        #region Fields
#pragma warning disable IDE0044
        [ANaughty.InfoBox("This option does not influence the \"C++ Compiler Configuration\" option in the player settings.", ANaughty.EInfoBoxType.Warning)]
        [SerializeField] [Tooltip("Enables optimizations for Neutron.")] private BuildMode _buildMode = BuildMode.Debug; // The build mode.
        [ANaughty.InfoBox("Neutron uses its own physics simulation system, you can implement your own simulation, see documentation.")]
        [ANaughty.InfoBox("This option is only valid for the default scene, this option is not valid for extra scenes created by Neutron.")]
        [SerializeField] [Box("Physics")] [Tooltip("Sets whether the physics should be simulated automatically or not.")] private bool _autoSimulation = false; // Used to enable/disable the physics simulation.
        [ANaughty.InfoBox("This option sets the physics mode that Neutron scenes should run.")]
        [SerializeField] [Tooltip("Provides options for 2D and 3D local physics.")] private PhysicsMode _physicsMode = PhysicsMode.Physics3D; // Used to set the physics mode.
        [SerializeField]
        [ANaughty.InfoBox("A lower value can penalize performance and very high values ​​can cause Stack Overflow.", ANaughty.EInfoBoxType.Warning)]
        [ANaughty.InfoBox("Stack Overflow cannot be caught by a try-catch block and the corresponding process is terminated.", ANaughty.EInfoBoxType.Warning)]
        [Box("Memory")]
        [Range(0, 2048)] [Tooltip("The maximum size of data that can be allocated on the stack.")] private int _stackAllocSize = 256; // The maximum stack allocation size.
        [ANaughty.InfoBox("This option is only valid for the pool of Neutron Stream.")]
        [ANaughty.InfoBox("The higher the value, the more RAM will be used, depending on the amount of objects in the pool.", ANaughty.EInfoBoxType.Warning)]
        [SerializeField] [Range(1, 65535)] [Tooltip("The maximum capacity of the Neutron Stream.")] private int _streamBufferSize = 256; // The maximum capacity of the Neutron Stream.
        [ANaughty.InfoBox("Incremental(GC) spreads out the process of garbage collection over multiple frames.")]
        [SerializeField] [Tooltip("This can significantly increase performance.")] private bool _incrementalGC = true; // Used to enable/disable the incremental GC.
        [SerializeField] [Box("References")] [ReadOnly] private StateSettings _settings; // The current settings of the Neutron.
#pragma warning restore IDE0044
        #endregion

#pragma warning disable IDE0051
        private void Awake()
#pragma warning restore IDE0051
        {
            MaxStackAllocSize = _stackAllocSize; // Set the maximum stack allocation size.
            UnityThreadId = ThreadHelper.GetThreadID(); // Get the Unity thread id.

            LoadSettings(); // Load the settings.
            LoadSynchronizationSettings(); // Load the synchronization settings.
            PreAllocPool(); // Pre-allocate the memory pool.
        }

        [Obsolete]
#pragma warning disable IDE0051
        private void Start()
#pragma warning restore IDE0051
        {
            Physics.autoSimulation = _autoSimulation; // Set the physics simulation.
#if UNITY_2020_1_OR_NEWER
            Physics2D.simulationMode = !_autoSimulation ? SimulationMode2D.Script : SimulationMode2D.FixedUpdate; // Set the physics simulation.
#else
            Physics2D.autoSimulation = _autoSimulation; // Set the physics simulation.
#endif
#if UNITY_SERVER && !UNITY_EDITOR
            Debug.unityLogger.logEnabled = true; // Disable the unity logger.
#endif
            GameObject controllers = GameObject.Find("Neutron Controllers"); // Find the controllers object.
            if (controllers != null)
            {
                ClientObject = controllers.transform.Find("Client(Your Client-Side scripts)").gameObject; // Find the client object.
                ServerObject = controllers.transform.Find("Server(Your Server-Side scripts)").gameObject;
            }
        }

#pragma warning disable IDE0051
        private void OnEnable() => DontDestroyOnLoad(transform.root);
#pragma warning restore IDE0051

        private void PreAllocPool()
        {
            // Pre-allocate the memory pool.

            Neutron.PooledNetworkStreams = new NeutronPool<NeutronStream>(() => new NeutronStream(true, _streamBufferSize), Settings.NeutronStream, false, "Neutron Streams");
            Neutron.PooledNetworkPackets = new NeutronPool<NeutronPacket>(() => new NeutronPacket(), Settings.NeutronPacket, false, "Neutron Packets");
            NeutronSocket.PooledSocketAsyncEventArgsForAccept = new NeutronPool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs(), Settings.AcceptPool, false, "Accept Pool");

            for (int i = 0; i < Settings.NeutronStream; i++)
                Neutron.PooledNetworkStreams.Push(new NeutronStream(true, _streamBufferSize));
            for (int i = 0; i < Settings.NeutronPacket; i++)
                Neutron.PooledNetworkPackets.Push(new NeutronPacket());
            for (int i = 0; i < Settings.AcceptPool; i++)
                NeutronSocket.PooledSocketAsyncEventArgsForAccept.Push(new SocketAsyncEventArgs());
        }

        private void LoadSettings()
        {
#if UNITY_EDITOR
            _settings = Resources.Load<StateSettings>("Editor - Neutron Settings"); // Load the settings.
#elif UNITY_SERVER
            _settings = Resources.Load<StateSettings>("Server - Neutron Settings");
#elif UNITY_ANDROID
            _settings = Resources.Load<StateSettings>("Android - Neutron Settings");
#elif UNITY_STANDALONE
            _settings = Resources.Load<StateSettings>("Standalone - Neutron Settings");
#endif
            if (Settings == null)
            {
                Settings = _settings;
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
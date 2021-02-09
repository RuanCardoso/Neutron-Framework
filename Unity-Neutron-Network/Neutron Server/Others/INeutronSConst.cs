using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Server.Cheats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Internal.Server
{
    public class NeutronSConst : MonoBehaviour // It inherits from MonoBehaviour because it is an instance of GameObject.
    {
        [SerializeField] private GameObject[] sharedObjects;
        [SerializeField] private GameObject[] unsharedObjects;
        [SerializeField] private bool ChannelsPhysics;
        [SerializeField] private bool RoomsPhysics;
        [SerializeField] private bool SharingOnChannels;
        [SerializeField] private bool SharingOnRooms;
        [SerializeField] private LocalPhysicsMode PhysicsMode = LocalPhysicsMode.None;

        public const string LOCAL_HOST = "http://127.0.0.1"; // local host.
        public static float TELEPORT_DISTANCE_TOLERANCE; // maximum teleport distance.
        public static float SPEEDHACK_TOLERANCE; // 0.1 = 0.1 x 1000 = 100 -> 1000/100 = 10 pckts per seconds.
        public static int MAX_RECEIVE_MESSAGE_SIZE; // max message receiv in server.
        public static int MAX_SEND_MESSAGE_SIZE; // max send receiv in server.
        public static int LIMIT_OF_CONNECTIONS_BY_IP; // limit of connections per ip.

        [NonSerialized] public Compression COMPRESSION_MODE = Compression.Deflate; // Level of compression of the bytes.
        public ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>(); // Thread-Safe - All shares that inherit from 
        public ConcurrentDictionary<TcpClient, Player> Players = new ConcurrentDictionary<TcpClient, Player>(); // thread safe - players of server.
        public ConcurrentDictionary<int, Channel> Channels = new ConcurrentDictionary<int, Channel>(); // thread safe - channels of server.
        protected List<IPAddress> blockedConnections = new List<IPAddress>();
        [SerializeField] private List<Channel> serializedChannels = new List<Channel>();

        private int serverPort; // port of server

        protected int FPS; // Frame rate of server. -1 for unlimited frame
        protected int DPF; // Data processing rate per frame/tick
        protected int sendRateTCP; // sendrate of tcp
        protected int sendRateUDP; // sendrate of udp
        protected int recRateTCP; // recrate of tcp
        protected int recRateUDP; // recrate of udp
        protected int backLog; // Maximum size of the acceptance queue for simultaneous clients
        protected bool noDelay; // Gets or sets a value that disables a delay when send or receive buffers are not full
        protected bool enableAntiCheat; // see documentation.
        protected bool dontDestroyOnLoad; // dont destroy server.
        protected bool _ready; // indicate server is up.

        protected static TcpListener _TCPListen;

        private void SerializeInspector()
        {
            for (int i = 0; i < serializedChannels.Count; i++)
            {
                Channel channel = serializedChannels[i];
                Channels.TryAdd(channel.ID, channel);
                Utils.CreateContainer($"[Container] -> Channel[{channel.ID}]", ChannelsPhysics, SharingOnChannels, sharedObjects, unsharedObjects, PhysicsMode);
                foreach (Room room in channel.GetRooms())
                {
                    Utils.CreateContainer($"[Container] -> Room[{room.ID}]", RoomsPhysics, SharingOnRooms, sharedObjects, unsharedObjects, PhysicsMode);
                }
            }
        }

        private void OnEnable()
        {
#if UNITY_2018_3_OR_NEWER
#if UNITY_SERVER
        Console.Clear();
#endif
#if UNITY_SERVER || UNITY_EDITOR
            SerializeInspector();
            IData IData = Data.LoadSettings();
            if (IData == null) Utils.LoggerError("Failed to initialize server -> error code: 0x1002");
            else
            {
                SetSetting(IData);
                _TCPListen = new TcpListener(new IPEndPoint(IPAddress.Any, serverPort)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                _TCPListen.Start(backLog);
                _ready = true;
            }
#endif
#else
            Console.WriteLine("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
            Utils.LoggerError("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
#endif
        }

        void SetSetting(IData Data)
        {
            TELEPORT_DISTANCE_TOLERANCE = Data.teleportTolerance;
            SPEEDHACK_TOLERANCE = Data.speedHackTolerance;
            MAX_RECEIVE_MESSAGE_SIZE = Data.max_rec_msg;
            MAX_SEND_MESSAGE_SIZE = Data.max_send_msg;
            LIMIT_OF_CONNECTIONS_BY_IP = Data.limit_of_conn_by_ip;

            COMPRESSION_MODE = (Compression)Data.compressionOptions;
            serverPort = Data.serverPort;
            FPS = Data.serverFPS;
            DPF = Data.serverDPF;
            sendRateTCP = Data.serverSendRate;
            sendRateUDP = Data.serverSendRateUDP;
            recRateTCP = Data.serverReceiveRate;
            recRateUDP = Data.serverReceiveRateUDP;
            backLog = Data.backLog;
            noDelay = Data.serverNoDelay;
            enableAntiCheat = Data.antiCheat;
            dontDestroyOnLoad = Data.dontDestroyOnLoad;

            CheatsUtils.enabled = enableAntiCheat;
        }
    }
}
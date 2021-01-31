using NeutronNetwork.Internal.Server.Cheats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Internal.Server
{
    public class NeutronSConst : MonoBehaviour // It inherits from MonoBehaviour because it is an instance of GameObject.
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public const string LOCAL_HOST = "http://127.0.0.1"; // local host.
        public static float TELEPORT_DISTANCE_TOLERANCE = 5f; // maximum teleport distance.
        public static float SPEEDHACK_TOLERANCE = 10f; // 0.1 = 0.1 x 1000 = 100 -> 1000/100 = 10 pckts per seconds.
        public static int MAX_RECEIVE_MESSAGE_SIZE = 512;
        public static int MAX_SEND_MESSAGE_SIZE = 512;
        public static int LIMIT_OF_CONNECTIONS_BY_IP = 2;
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [NonSerialized] public Compression COMPRESSION_MODE = Compression.Deflate; // Level of compression of the bytes.
        [NonSerialized] public ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>(); // Thread-Safe - All shares that inherit from 
        [NonSerialized] public ConcurrentDictionary<TcpClient, Player> Players = new ConcurrentDictionary<TcpClient, Player>();
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [SerializeField] private List<Channel> serializedChannels = new List<Channel>();
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private int serverPort = 5055;
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected int FPS = 60; // Frame rate of server. -1 for unlimited frame
        protected int DPF = 30; // Data processing rate per frame/tick
        protected int backLog = 10; // Maximum size of the acceptance queue for simultaneous clients
        protected bool noDelay = false; // Gets or sets a value that disables a delay when send or receive buffers are not full
        protected bool enableAntiCheat = true; // see documentation.
        protected bool dontDestroyOnLoad = true; // dont destroy server.
        protected bool _ready = false;
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected ConcurrentDictionary<int, Channel> Channels = new ConcurrentDictionary<int, Channel>();
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected static TcpListener _TCPListen;
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SerializeInspector()
        {
            for (int i = 0; i < serializedChannels.Count; i++)
            {
                Channel channel = serializedChannels[i];
                Channels.TryAdd(channel.ID, channel);
            }
        }

        private void OnEnable()
        {
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
        }

        void SetSetting(IData Data)
        {
            TELEPORT_DISTANCE_TOLERANCE = Data.teleportTolerance;
            SPEEDHACK_TOLERANCE = Data.speedHackTolerance;
            MAX_RECEIVE_MESSAGE_SIZE = Data.max_rec_msg;
            MAX_SEND_MESSAGE_SIZE = Data.max_send_msg;
            LIMIT_OF_CONNECTIONS_BY_IP = Data.limit_of_conn_by_ip;

            //COMPRESSION_MODE = (Compression)Data.compressionOptions;
            //serverPort = Data.serverPort;
            //backLog = Data.backLog;
            //FPS = Data.serverFPS;
            //DPF = Data.serverDPF;
            //noDelay = Data.serverNoDelay;
            //enableAntiCheat = Data.antiCheat;
            //dontDestroyOnLoad = Data.dontDestroyOnLoad;
            //CheatsUtils.enabled = enableAntiCheat;
        }
    }
}
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Server.Cheats;
using NeutronNetwork.Internal.Server.InternalEvents;
using NeutronNetwork.Internal.Wrappers;
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
    public class NeutronServerConstants : MonoBehaviour // It inherits from MonoBehaviour because it is an instance of GameObject.
    {
        public static event SEvents.OnServerAwake onServerAwake;
        protected static TcpListener ServerSocket;

        public static float TELEPORT_DISTANCE_TOLERANCE; // maximum teleport distance.
        public static float SPEEDHACK_TOLERANCE; // 0.1 = 0.1 x 1000 = 100 -> 1000/100 = 10 pckts per seconds.
        public static int MAX_RECEIVE_MESSAGE_SIZE; // max message receiv in server.
        public static int MAX_SEND_MESSAGE_SIZE; // max send receiv in server.
        public static int LIMIT_OF_CONNECTIONS_BY_IP; // limit of connections per ip.

        [SerializeField] private GameObject[] sharedObjects;
        [SerializeField] private GameObject[] unsharedObjects;
        [SerializeField] private bool ChannelPhysics;
        [SerializeField] private bool RoomPhysics;
        [SerializeField] private bool SharingOnChannels;
        [SerializeField] private bool SharingOnRooms;
        [SerializeField] private LocalPhysicsMode PhysicsMode = LocalPhysicsMode.None;
        [SerializeField] private List<Channel> serializedChannels = new List<Channel>();

        public NeutronSafeDictionary<TcpClient, Player> Players = new NeutronSafeDictionary<TcpClient, Player>(); // thread safe - players of server.
        public NeutronSafeDictionary<int, Channel> Channels = new NeutronSafeDictionary<int, Channel>(); // thread safe - channels of server.
        protected NeutronSafeDictionary<string, int> SYN = new NeutronSafeDictionary<string, int>();
        public NeutronQueue<Action> monoActions = new NeutronQueue<Action>(); // Thread-Safe - All shares that inherit from 
        protected bool _ready; // indicate server is up.
        public static IData IData;

        private void SerializeInspector()
        {
            for (int i = 0; i < serializedChannels.Count; i++)
            {
                Channel channel = serializedChannels[i];
                Channels.TryAdd(channel.ID, channel);
                Utils.CreateContainer($"[Container] -> Channel[{channel.ID}]", ChannelPhysics, SharingOnChannels, sharedObjects, unsharedObjects, PhysicsMode);
                foreach (Room room in channel.GetRooms())
                {
                    Utils.CreateContainer($"[Container] -> Room[{room.ID}]", RoomPhysics, SharingOnRooms, sharedObjects, unsharedObjects, PhysicsMode);
                }
            }
        }

        void InitializeEvents()
        {
            GetComponent<NeutronEvents>().Initialize();
        }

        public void Awake()
        {
#if UNITY_2018_3_OR_NEWER
#if UNITY_SERVER
        Console.Clear();
#endif
#if UNITY_SERVER || UNITY_EDITOR
            InitializeEvents();
            onServerAwake?.Invoke();
            SerializeInspector();
            IData IData = Data.LoadSettings();
            if (IData == null) Utils.LoggerError("Failed to initialize server");
            else
            {
                try
                {
                    SetSetting(IData);
                    ServerSocket = new TcpListener(new IPEndPoint(IPAddress.Any, IData.serverPort)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                    ServerSocket.Start(IData.backLog);
                    _ready = true;
                }
                catch (Exception ex) { Utils.LoggerError(ex.Message); }
            }
#endif
#else
            Console.WriteLine("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
            Utils.LoggerError("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
#endif
        }

        void SetSetting(IData Data)
        {
            IData = Data;

            TELEPORT_DISTANCE_TOLERANCE = Data.teleportTolerance;
            SPEEDHACK_TOLERANCE = Data.speedHackTolerance;
            MAX_RECEIVE_MESSAGE_SIZE = Data.max_rec_msg;
            MAX_SEND_MESSAGE_SIZE = Data.max_send_msg;
            LIMIT_OF_CONNECTIONS_BY_IP = Data.limit_of_conn_by_ip;

            CheatsUtils.enabled = Data.antiCheat;
        }
    }
}
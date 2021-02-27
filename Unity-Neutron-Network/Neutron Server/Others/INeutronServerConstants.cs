﻿using NeutronNetwork.Internal.Attributes;
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
        public static event ServerEvents.OnServerAwake onServerAwake;
        protected static TcpListener ServerSocket;

        public static Compression COMPRESSION_MODE;
        public static float TELEPORT_DISTANCE_TOLERANCE; // maximum teleport distance.
        public static float SPEEDHACK_TOLERANCE; // 0.1 = 0.1 x 1000 = 100 -> 1000/100 = 10 pckts per seconds.
        public static int MAX_RECEIVE_MESSAGE_SIZE; // max message receiv in server.
        public static int MAX_SEND_MESSAGE_SIZE; // max send receive in server.
        public static int LIMIT_OF_CONNECTIONS_BY_IP; // limit of connections per ip.

        public NeutronQueue<Action> monoActions = new NeutronQueue<Action>(); // Thread-Safe - All shares that inherit from 
        public NeutronSafeDictionary<TcpClient, Player> Players = new NeutronSafeDictionary<TcpClient, Player>(); // thread safe - players of server.
        public NeutronSafeDictionary<int, Channel> Channels = new NeutronSafeDictionary<int, Channel>(); // thread safe - channels of server.
        protected NeutronSafeDictionary<string, int> SYN = new NeutronSafeDictionary<string, int>();

        [SerializeField] private List<Channel> _Channels = new List<Channel>();
        [SerializeField] private GameObject[] sharedObjects;
        [SerializeField] private GameObject[] unsharedObjects;
        [SerializeField] private bool ChannelPhysics;
        [SerializeField] private bool RoomPhysics;
        [SerializeField] private bool SharingOnChannels;
        [SerializeField] private bool SharingOnRooms;
        [SerializeField] private LocalPhysicsMode PhysicsMode = LocalPhysicsMode.None;

        protected bool _ready; // indicate server is up.

        private void SerializeInspector()
        {
            for (int i = 0; i < _Channels.Count; i++)
            {
                Channel channel = _Channels[i];
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
            NeutronConfig.LoadSettings();
            InitializeEvents();
            onServerAwake?.Invoke();
            SerializeInspector();
            if (NeutronConfig.GetConfig == null) Utilities.LoggerError("Failed to initialize server");
            else
            {
                try
                {
                    SetSetting(NeutronConfig.GetConfig);
                    ServerSocket = new TcpListener(new IPEndPoint(IPAddress.Any, NeutronConfig.GetConfig.serverPort)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                    ServerSocket.Start(NeutronConfig.GetConfig.backLog);
                    _ready = true;
                }
                catch (Exception ex) { Utilities.LoggerError(ex.Message); }
            }
#endif
#else
            Console.WriteLine("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
            Utilities.LoggerError("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
#endif
        }

        void SetSetting(JsonData Data)
        {
            COMPRESSION_MODE = (Compression)Data.compressionOptions;

            TELEPORT_DISTANCE_TOLERANCE = Data.teleportTolerance;
            SPEEDHACK_TOLERANCE = Data.speedHackTolerance;
            MAX_RECEIVE_MESSAGE_SIZE = Data.max_rec_msg;
            MAX_SEND_MESSAGE_SIZE = Data.max_send_msg;
            LIMIT_OF_CONNECTIONS_BY_IP = Data.limit_of_conn_by_ip;

            CheatsUtils.enabled = Data.antiCheat;
        }
    }
}
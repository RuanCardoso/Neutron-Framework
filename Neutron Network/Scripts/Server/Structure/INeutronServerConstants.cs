using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Server.Cheats;
using NeutronNetwork.Internal.Server.Delegates;
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
    public class NeutronServerConstants : MonoBehaviour
    {
        #region Socket
        public TcpListener TcpSocket;
        #endregion

        #region Constants
        public static int MAX_RECEIVE_MESSAGE_SIZE;
        public static int MAX_SEND_MESSAGE_SIZE;
        public static int LIMIT_OF_CONNECTIONS_BY_IP;
        #endregion

        #region Collections
        public ChannelDictionary ChannelsById = new ChannelDictionary();
        public NeutronSafeDictionary<TcpClient, Player> PlayersBySocket = new NeutronSafeDictionary<TcpClient, Player>();
        public NeutronSafeDictionary<int, Player> PlayersById = new NeutronSafeDictionary<int, Player>();
        public NeutronSafeDictionary<string, int> RegisteredConnectionsByIp = new NeutronSafeDictionary<string, int>();
        public NeutronQueue<Action> ActionsDispatcher = new NeutronQueue<Action>();
        #endregion

        #region Physics
        public GameObject[] unsharedObjects;
        [Separator] public LocalPhysicsMode PhysicsMode = LocalPhysicsMode.Physics3D;
        #endregion

        #region Variables
        private bool m_IsReady;
        public bool IsReady { get => m_IsReady; set => m_IsReady = value; }

        [SerializeField] [ReadOnly] private int m_CurrentPlayers;
        public int CurrentPlayers { get => m_CurrentPlayers; set => m_CurrentPlayers = value; }
        #endregion

        public void Awake()
        {
#if UNITY_2018_3_OR_NEWER
#if UNITY_SERVER
        Console.Clear();
#endif
#if UNITY_SERVER || UNITY_EDITOR
            if (NeutronConfig.Settings != null)
            {
                try
                {
                    SetConstants(NeutronConfig.Settings);
                    TcpSocket = new TcpListener(new IPEndPoint(IPAddress.Any, NeutronConfig.Settings.GlobalSettings.Port)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                    TcpSocket.Start(NeutronConfig.Settings.ServerSettings.BackLog);
                    IsReady = true;
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10048)
                        enabled = NeutronUtils.LoggerError("This Server instance has been disabled, because another instance is in use.");
                    else NeutronUtils.LoggerError(ex.Message);
                }
            }
#endif
#else
            NeutronUtils.LoggerError("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.3.");
#endif
        }

        private void SetConstants(NeutronSettings NeutronSettings)
        {
            CheatsUtils.enabled = NeutronSettings.ServerSettings.NeutronAntiCheat;
            MAX_RECEIVE_MESSAGE_SIZE = NeutronSettings.MAX_REC_MSG;
            MAX_SEND_MESSAGE_SIZE = NeutronSettings.MAX_SEND_MSG;
            LIMIT_OF_CONNECTIONS_BY_IP = NeutronSettings.LIMIT_OF_CONN_BY_IP;
        }
    }
}
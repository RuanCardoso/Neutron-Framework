using NeutronNetwork.Attributes;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Wrappers;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Server
{
    public class ServerBehaviour : MonoBehaviour
    {
        #region Socket
        public TcpListener TcpSocket;
        #endregion

        #region Constants
        public static int MAX_RECEIVE_MESSAGE_SIZE;
        public static int LIMIT_OF_CONNECTIONS_BY_IP;
        #endregion

        #region Collections
        public ChannelDictionary ChannelsById = new ChannelDictionary();
        public NeutronSafeDictionary<TcpClient, NeutronPlayer> PlayersBySocket = new NeutronSafeDictionary<TcpClient, NeutronPlayer>();
        public NeutronSafeDictionary<int, NeutronPlayer> PlayersById = new NeutronSafeDictionary<int, NeutronPlayer>();
        public NeutronSafeDictionary<string, int> RegisteredConnectionsByIp = new NeutronSafeDictionary<string, int>();
        public NeutronQueue<Action> ActionsDispatcher = new NeutronQueue<Action>();
        #endregion

        #region Physics
        public GameObject[] unsharedObjects;
        [HorizontalLineDown] public LocalPhysicsMode PhysicsMode = LocalPhysicsMode.Physics3D;
        #endregion

        #region Variables
        public bool IsReady { get; set; }
        public int CurrentPlayers;
        #endregion

        #region Threading
        //* ID de Identificação do Thread.
        protected int ServerDataProcessingStackManagedThreadId;
        #endregion

        public void Awake()
        {
#if UNITY_2018_4_OR_NEWER
#if UNITY_SERVER
        Console.Clear();
#endif
#if UNITY_SERVER || UNITY_EDITOR
            if (NeutronMain.Settings != null && (NeutronMain.Settings.GlobalSettings.Addresses[0] == "127.0.0.1" || NeutronMain.Settings.GlobalSettings.Addresses[0].Equals("localhost", StringComparison.InvariantCultureIgnoreCase)))
            {
                try
                {
                    #region Constants
                    MAX_RECEIVE_MESSAGE_SIZE = NeutronMain.Settings.MAX_REC_MSG;
                    LIMIT_OF_CONNECTIONS_BY_IP = NeutronMain.Settings.LIMIT_OF_CONN_BY_IP;
                    #endregion

                    #region Socket
                    TcpSocket = new TcpListener(new IPEndPoint(IPAddress.Any, NeutronMain.Settings.GlobalSettings.Port)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                    TcpSocket.Start(NeutronMain.Settings.ServerSettings.BackLog);
                    IsReady = true;
                    #endregion
                }
                catch (SocketException ex)
                {
                    IsReady = false;
                    if (ex.ErrorCode == 10048)
                        LogHelper.Error("This Server instance has been disabled, because another instance is in use.");
                    else
                        LogHelper.Error(ex.Message);
                }
            }
            else LogHelper.Error("This Server instance has been disabled, because another instance is in use.");
#endif
#else
            NeutronLogger.LoggerError("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.4.");
#endif
        }
    }
}
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
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
        public TcpListener TcpListener {
            get;
            private set;
        }
        #endregion

        #region Collections
        [Label("Channels")] public ChannelDictionary ChannelsById = new ChannelDictionary();
        public NeutronSafeDictionary<TcpClient, NeutronPlayer> PlayersBySocket = new NeutronSafeDictionary<TcpClient, NeutronPlayer>();
        public NeutronSafeDictionary<int, NeutronPlayer> PlayersById = new NeutronSafeDictionary<int, NeutronPlayer>();
        public NeutronSafeDictionary<string, int> RegisteredConnectionsByIp = new NeutronSafeDictionary<string, int>();
        #endregion

        #region Fields
        [HorizontalLine] public LocalPhysicsMode _localPhysicsMode = LocalPhysicsMode.Physics3D;
        [ReadOnly] public PlayerGlobalController _playerGlobalController;
        [ReadOnly] public ServerSide _serverSideController;
        public bool _enableActionsOnChannel;
        public bool _serverOwnsTheMatchManager;
        public bool _serverOwnsTheSceneObjects;
        [ReadOnly] public NeutronBehaviour[] _actions;
        [ReadOnly] [HorizontalLine] public int _playerCount;
        #endregion

        #region Properties
        public bool IsReady {
            get;
            set;
        }
        protected ThreadManager ThreadManager { get; } = new ThreadManager();
        #endregion

        private void Controllers()
        {
            _actions = transform.root.GetComponentsInChildren<NeutronBehaviour>();
            if (_serverSideController == null)
            {
                _serverSideController = transform.root.GetComponentInChildren<ServerSide>();
                if (_serverSideController == null)
                    throw new NeutronException("Server Side Controller not defined!");
            }
            if (_playerGlobalController == null)
            {
                _playerGlobalController = transform.root.GetComponentInChildren<PlayerGlobalController>();
                if (_playerGlobalController == null)
                    throw new NeutronException("Player Global Controller not defined!");
            }
        }

        protected virtual void Awake()
        {
#if UNITY_2018_4_OR_NEWER
            Controllers();
#if UNITY_SERVER && !UNITY_EDITOR
        Console.Clear();
#endif
#if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            if (NeutronModule.Settings != null)
            {
                try
                {
                    TcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, NeutronModule.Settings.GlobalSettings.Port)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                    TcpListener.Start(NeutronModule.Settings.ServerSettings.BackLog);
                    //* Marca o servidor como pronto para inicialização.
                    IsReady = true;
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10048)
                        LogHelper.Info("This server instance has been disabled, because another instance is in use.");
                    else
                        throw new Exception(ex.Message);
                }
            }
            else
                LogHelper.Error("Settings is missing!");
#endif
#else
                LogHelper.Error("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.4.");
#endif
        }
    }
}
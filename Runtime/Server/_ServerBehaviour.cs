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
        [SerializeField] [HorizontalLine] private LocalPhysicsMode _localPhysicsMode = LocalPhysicsMode.Physics3D;
        [SerializeField] [ReadOnly] private PlayerGlobalController _playerGlobalController;
        [SerializeField] [ReadOnly] private ServerSide _serverSideController;
        [SerializeField] private bool _enableActionsOnChannel;
        [SerializeField] private bool _serverOwnsTheMatchManager = true;
        [SerializeField] private bool _serverOwnsTheSceneObjects = true;
        [SerializeField] [ReadOnly] private NeutronBehaviour[] _actions;
        [SerializeField] private string[] _scenes;
        [ReadOnly] [HorizontalLine] public int _playerCount;
        #endregion

        #region Properties
        public bool IsReady {
            get;
            private set;
        }

        protected ThreadManager ThreadManager {
            get;
        } = new ThreadManager();

        public LocalPhysicsMode LocalPhysicsMode {
            get => _localPhysicsMode;
        }

        public PlayerGlobalController PlayerGlobalController {
            get => _playerGlobalController;
        }

        public ServerSide ServerSideController {
            get => _serverSideController;
        }

        public bool EnableActionsOnChannel {
            get => _enableActionsOnChannel;
        }

        public bool ServerOwnsTheMatchManager {
            get => _serverOwnsTheMatchManager;
        }

        public bool ServerOwnsTheSceneObjects {
            get => _serverOwnsTheSceneObjects;
        }

        public NeutronBehaviour[] Actions {
            get => _actions;
        }
        #endregion

        private void Controllers()
        {
            _actions = transform.root.GetComponentsInChildren<NeutronBehaviour>();
            if (ServerSideController == null)
            {
                _serverSideController = transform.root.GetComponentInChildren<ServerSide>();
                if (ServerSideController == null)
                    throw new NeutronException("Server Side Controller not defined!");
            }
            if (PlayerGlobalController == null)
            {
                _playerGlobalController = transform.root.GetComponentInChildren<PlayerGlobalController>();
                if (PlayerGlobalController == null)
                    throw new NeutronException("Player Global Controller not defined!");
            }
        }

        private void LoadScenes()
        {
            SceneManager.sceneLoaded += OnLoadScene;
            foreach (string sceneName in _scenes)
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.isLoaded)
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            }
        }

        private void OnLoadScene(Scene scene, LoadSceneMode mode)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in rootGameObjects)
            {
                if (mode == LoadSceneMode.Additive)
                {
                    if (!(gameObject.GetComponent<NeutronView>() != null))
                        Destroy(gameObject);
                }
            }

            if (_scenes.Length > 0)
            {
                if (scene.name == _scenes[_scenes.Length - 1])
                    SceneManager.sceneLoaded -= OnLoadScene;
            }
            else
                SceneManager.sceneLoaded -= OnLoadScene;
        }

        protected virtual void Awake()
        {
#if UNITY_2018_4_OR_NEWER
#if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            Controllers();
            LoadScenes();
#endif
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
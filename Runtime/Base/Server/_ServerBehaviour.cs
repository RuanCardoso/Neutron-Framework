using MarkupAttributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Components;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Packets;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Server
{
    public class ServerBehaviour : MarkupBehaviour
    {
        #region Socket
        public TcpListener TcpListener
        {
            get;
            private set;
        }
        #endregion

        [SerializeField] [Box("Server Settings")] protected bool _autoStart = true;
        [SerializeField] [HideInInspector] protected PhysicsMode _localPhysicsMode = PhysicsMode.Physics3D;

        [SerializeField] [Naughty.Attributes.Scene] protected string[] _scenes;
        [ReadOnly] public int _playerCount;

        [SerializeField] [Box("Global Matchmaking")] protected NeutronGlobal _globalMatchmaking;
        [Box("Local Matchmaking")] [Rename("Channels")] public ChannelDictionary ChannelsById = new();

        #region Collections
        public NeutronSafeDictionary<TcpClient, NeutronPlayer> PlayersBySocket = new();
        public NeutronSafeDictionary<int, NeutronPlayer> PlayersById = new();
        public NeutronSafeDictionary<string, int> RegisteredConnectionsByIp = new();
        #endregion

        [SerializeField] [Box("Matchmaking Settings")] [Rename("Mode")] protected MatchmakingMode _matchmakingMode = MatchmakingMode.Room;
        [SerializeField] [Rename("Scene Obj's Owner")] protected OwnerMode _sceneObjectsOwner = OwnerMode.Server;
        [SerializeField] [Rename("Manager Owner")] protected OwnerMode _matchmakingManagerOwner = OwnerMode.Server;

        [SerializeField] [Foldout("References")] [ReadOnly] [Rename("Player GC")] protected PlayerGlobalController _playerGlobalController;
        [SerializeField] [ReadOnly] [Rename("Server SC")] protected ServerSide _serverSideController;
        [SerializeField] [ReadOnly] protected NeutronBehaviour[] _neutronBehaviours;

        #region Properties
        public bool IsReady
        {
            get;
            private set;
        }

        protected ThreadManager ThreadManager
        {
            get;
        } = new ThreadManager();

        public LocalPhysicsMode LocalPhysicsMode
        {
            get => (LocalPhysicsMode)_localPhysicsMode;
        }

        public PlayerGlobalController PlayerGlobalController
        {
            get => _playerGlobalController;
        }

        public ServerSide ServerSideController
        {
            get => _serverSideController;
        }

        public MatchmakingMode MatchmakingMode
        {
            get => _matchmakingMode;
        }

        public OwnerMode MatchmakingManagerOwner
        {
            get => _matchmakingManagerOwner;
        }

        public OwnerMode SceneObjectsOwner
        {
            get => _sceneObjectsOwner;
        }

        public NeutronBehaviour[] Actions
        {
            get => _neutronBehaviours;
        }

        public bool AutoStart => _autoStart;
        #endregion

        private void Controllers()
        {
            if (ServerSideController == null)
            {
                _serverSideController = transform.root.GetComponentInChildren<ServerSide>();
                if (ServerSideController == null)
                    throw new NeutronException("Server controller not defined!");
            }

            if (PlayerGlobalController == null)
            {
                _playerGlobalController = transform.root.GetComponentInChildren<PlayerGlobalController>();
                if (PlayerGlobalController == null)
                    throw new NeutronException("Player controller not defined!");
            }
        }

        private void GetActions()
        {
            _neutronBehaviours = transform.root.GetComponentsInChildren<NeutronBehaviour>();
        }

        [Naughty.Attributes.Button("Setup Scenes", Naughty.Attributes.EButtonEnableMode.Editor)]
        private void LoadScenes()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorSceneManager.sceneOpened += OnOpenScene;
#endif

            foreach (string sceneName in _scenes)
            {
                if (Application.isPlaying)
                {
#if !UNITY_SERVER && !UNITY_EDITOR
                    var rootGameObjects = SceneManager.GetSceneByName(sceneName).GetRootGameObjects();
                    foreach (var gameObject in rootGameObjects)
                        Destroy(gameObject);
#endif
                }
                else
                {
#if UNITY_EDITOR
                    string path = EditorBuildSettings.scenes.First(x => x.path.Contains(sceneName)).path;
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
#endif
                }
            }
        }

        private void OnLoadScene(Scene scene, LoadSceneMode mode)
        {
            HybridOnLoadScene(scene, (int)mode);
        }

#if UNITY_EDITOR
        private void OnOpenScene(Scene scene, OpenSceneMode mode)
        {
            HybridOnLoadScene(scene, (int)mode);
        }
#endif

        private void HybridOnLoadScene(Scene scene, int mode)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in rootGameObjects)
            {
#if UNITY_EDITOR
                if (((LoadSceneMode)mode == LoadSceneMode.Additive) || ((OpenSceneMode)mode == OpenSceneMode.Additive))
#else
                if (((LoadSceneMode)mode == LoadSceneMode.Additive))
#endif
                {
                    if (!(gameObject.GetComponent<AllowOnServer>() != null))
                    {
                        if (Application.isPlaying)
                            Destroy(gameObject);
                        else
                            DestroyImmediate(gameObject, false);
                    }
                }
            }

            SceneHelper.MoveToContainer(new GameObject("(Server-Side)"), scene);

            if (_scenes.Length > 0)
            {
                if (scene.name == _scenes[^1])
                {
                    if (Application.isPlaying)
                        SceneManager.sceneLoaded -= OnLoadScene;
#if UNITY_EDITOR
                    else
                        EditorSceneManager.sceneOpened -= OnOpenScene;
#endif
                }
            }
            else
            {
                if (Application.isPlaying)
                    SceneManager.sceneLoaded -= OnLoadScene;
#if UNITY_EDITOR
                else
                    EditorSceneManager.sceneOpened -= OnOpenScene;
#endif
            }
        }

        protected void StartSocket()
        {
            if (!IsReady)
            {
#if UNITY_2018_4_OR_NEWER
                //* Its Ok
#if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
                Load();
                LoadScenes();
#endif
#if UNITY_SERVER && !UNITY_EDITOR
        Console.Clear();
        Console.Title = "Neutron Server";
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
                            LogHelper.Info("The local server has been disabled because another instance is running on the same machine.");
                        else
                            throw new Exception(ex.Message);
                    }
                }
                else
                    IsReady = LogHelper.Error("Settings is missing!");
#endif
#else
                    IsReady = LogHelper.Error("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.4.");
#endif
            }
            else
                IsReady = LogHelper.Error("The server has already been initialized!");
        }

        protected virtual void Awake()
        {
#if !UNITY_SERVER || UNITY_EDITOR
            if (_autoStart)
                StartSocket();
#else
            StartSocket();
#endif
        }

#if UNITY_EDITOR
        private void Reset()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            enabled = true;
            Load();
        }
#endif

        private void Load()
        {
            Controllers();
            GetActions();
        }
    }
}
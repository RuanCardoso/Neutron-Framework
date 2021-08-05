using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Server.Internal;
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
        public TcpListener TcpListener;
        #endregion

        #region Collections
        [Label("Channels")] public ChannelDictionary ChannelsById = new ChannelDictionary();
        public NeutronSafeDictionary<TcpClient, NeutronPlayer> PlayersBySocket = new NeutronSafeDictionary<TcpClient, NeutronPlayer>();
        public NeutronSafeDictionary<int, NeutronPlayer> PlayersById = new NeutronSafeDictionary<int, NeutronPlayer>();
        public NeutronSafeDictionary<string, int> RegisteredConnectionsByIp = new NeutronSafeDictionary<string, int>();
        #endregion

        #region Fields
        public GameObject[] DestroyObjects;
        [HorizontalLine] public LocalPhysicsMode Physics = LocalPhysicsMode.Physics3D;
        public bool ClientHasPhysics = true;
        public View View;
        public EventsBehaviour EventsBehaviour;
        [ReadOnly] [HorizontalLine] public int PlayerCount;
        #endregion

        #region Properties
        public bool IsReady { get; set; }
        public int PacketProcessingStack_ManagedThreadId { get; set; }
        #endregion

        public void Awake()
        {
#if UNITY_2018_4_OR_NEWER
            if (EventsBehaviour == null)
            {
                if (ServerBase.OnAwake == null)
                    EventsBehaviour = gameObject.AddComponent<EventsBehaviour>();
                else if (!LogHelper.Error("Events Behaviour not defined!"))
                    return;
                else
                    return;
            }
#if UNITY_SERVER
        Console.Clear();
#endif
#if UNITY_SERVER || UNITY_EDITOR
            if (NeutronModule.Settings != null)
            {
                try
                {
                    TcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, NeutronModule.Settings.GlobalSettings.Port)); // Server IP Address and Port. Note: Providers like Amazon, Google, Azure, etc ... require that the ports be released on the VPS firewall and In Server Management, servers that have routers, require the same process.
                    TcpListener.Start(NeutronModule.Settings.ServerSettings.BackLog);
                    IsReady = true;
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10048)
                        LogHelper.Error("This Server instance has been disabled, because another instance is in use.");
                    else
                        LogHelper.Error(ex.Message);
                }
            }
            else
                LogHelper.Error("Settings is missing!");
#endif
#else
            NeutronLogger.LoggerError("This version of Unity is not compatible with this asset, please use a version equal to or greater than 2018.4.");
#endif
        }
    }
}
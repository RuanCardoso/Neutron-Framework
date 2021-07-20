using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Wrappers;
using UnityEngine;

namespace NeutronNetwork.Client
{
    public class ClientBehaviour : MonoBehaviour
    {
        #region Socket
        protected TcpClient TcpSocket;
        protected UdpClient UdpSocket;
        #endregion

        #region Collections
        public NeutronQueue<Action> ActionsDispatcher = new NeutronQueue<Action>();
        public NeutronSafeDictionary<int, NeutronView> NetworkObjects = new NeutronSafeDictionary<int, NeutronView>();
        public NeutronSafeDictionary<int, NeutronPlayer> PlayerConnections = new NeutronSafeDictionary<int, NeutronPlayer>();
        #endregion

        #region Variables
        protected IPEndPoint _udpEndPoint;
        #endregion

        #region Threading
        protected CancellationTokenSource _cts = new CancellationTokenSource();
        #endregion

        public void Initialize()
        {
            NeutronPlayer pServer = new NeutronPlayer(0)
            {
                IsServer = true,
                Nickname = "Server",
            };

            #region Provider
            if (PlayerConnections.TryAdd(0, pServer))
            {
                for (int i = 0; i < NeutronMain.Settings.GlobalSettings.MaxPlayers; i++)
                {
                    int Key = (NeutronConstants.GENERATE_PLAYER_ID + i) + 1;
                    PlayerConnections.TryAdd(Key, new NeutronPlayer(Key));
                }
            }
            #endregion

            TcpSocket = new TcpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Tcp)));
            UdpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));
        }

        public void Dispose()
        {
            _cts.Cancel();
            TcpSocket.Dispose();
            UdpSocket.Dispose();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }
    }
}
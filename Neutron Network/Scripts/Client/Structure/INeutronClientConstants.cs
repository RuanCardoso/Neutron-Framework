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
    public class NeutronClientConstants : MonoBehaviour
    {
        #region Socket
        protected TcpClient TcpSocket;
        protected UdpClient UdpSocket;
        #endregion

        #region Collections
        public NeutronQueue<Action> ActionsDispatcher = new NeutronQueue<Action>();
        public NeutronSafeDictionary<int, NeutronView> NetworkObjects = new NeutronSafeDictionary<int, NeutronView>();
        public NeutronSafeDictionary<int, Player> PlayerConnections = new NeutronSafeDictionary<int, Player>();
        #endregion

        #region Variables
        protected IPEndPoint UDPEndPoint;
        #endregion

        #region Threading
        protected CancellationTokenSource _cts = new CancellationTokenSource();
        #endregion

        public void InitializeSocket()
        {
            #region Provider

            #region Server
            Player pServer = new Player(0);
            pServer.IsServer = true;
            pServer.Nickname = "Server";
            PlayerConnections.TryAdd(0, pServer);
            #endregion
            for (int i = 0; i < NeutronConfig.Settings.GlobalSettings.MaxPlayers; i++)
            {
                int Key = (NeutronConstants.GENERATE_PLAYER_ID + i) + 1;
                PlayerConnections.TryAdd(Key, new Player(Key));
            }
            #endregion

            TcpSocket = new TcpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Tcp)));
            UdpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));
        }

        public void Dispose()
        {
            using (_cts)
            using (UdpSocket)
            using (TcpSocket)
            {
                try
                {
                    Neutron.Client.IsConnected = false;
                    {
                        _cts.Cancel();
                    }
                }
                catch (ObjectDisposedException) { }
            }
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }
    }
}
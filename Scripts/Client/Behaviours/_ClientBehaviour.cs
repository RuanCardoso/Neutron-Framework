﻿using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Client
{
    public class ClientBehaviour
    {
        #region Socket
        protected TcpClient TcpClient { get; set; }
        protected UdpClient UdpClient { get; set; }
        protected NonAllocEndPoint UdpEndPoint { get; set; }
        protected CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();
        #endregion

        #region Collections
        public NeutronSafeDictionary<int, NeutronPlayer> Players { get; set; } = new NeutronSafeDictionary<int, NeutronPlayer>();
        #endregion

        protected void Initialize()
        {
            #region Provider
            if (Players.TryAdd(0, NeutronServer.Player))
            {
                for (int i = 0; i < NeutronModule.Settings.GlobalSettings.MaxPlayers; i++)
                {
                    int id = (NeutronConstantsSettings.GENERATE_PLAYER_ID + i) + 1;
                    if (Players.TryAdd(id, new NeutronPlayer()
                    {
                        ID = id,
                    })) { }
                }
            }
            #endregion

            int port = SocketHelper.GetFreePort(Protocol.Tcp);
            TcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, port));
            UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));

#if UNITY_EDITOR
            Application.quitting += OnQuit;
#endif
        }

        protected void Dispose()
        {
            TokenSource.Cancel();
            TcpClient.Dispose();
            UdpClient.Dispose();
        }

        private void OnQuit() => Dispose();
    }
}
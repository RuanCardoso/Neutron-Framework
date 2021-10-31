using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Packets;
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
        protected TcpClient TcpClient
        {
            get;
            private set;
        }

        protected UdpClient UdpClient
        {
            get;
            private set;
        }

        protected NonAllocEndPoint UdpEndPoint
        {
            get;
            set;
        }

        protected CancellationTokenSource TokenSource
        {
            get;
        } = new CancellationTokenSource();
        #endregion

        #region Collections
        public NeutronSafeDictionary<int, NeutronPlayer> Players
        {
            get;
        } = new NeutronSafeDictionary<int, NeutronPlayer>();

        protected ThreadManager ThreadManager { get; } = new ThreadManager();
        #endregion

        #region Matchmaking
        protected NeutronChannel NeutronChannel
        {
            get;
        } = new NeutronChannel();

        protected NeutronRoom NeutronRoom
        {
            get;
        } = new NeutronRoom();
        #endregion

        #region Functions
        protected void StartSocket()
        {
            #region Provider
            if (Players.TryAdd(0, PlayerHelper.MakeTheServerPlayer()))
            {
                for (int i = 0; i < NeutronModule.Settings.GlobalSettings.MaxPlayers; i++)
                {
                    int id = (NeutronConstantsSettings.GENERATE_PLAYER_ID + i) + 1;
                    if (Players.TryAdd(id, new NeutronPlayer()
                    {
                        Id = id,
                        Nickname = $"Client#{id}"
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
        #endregion
    }
}
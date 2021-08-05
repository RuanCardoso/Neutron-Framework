using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Server;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Constants;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Client
{
    public class ClientBehaviour/* : MonoBehaviour*/
    {
        #region Socket
        protected TcpClient TcpClient;
        protected UdpClient UdpClient;
        #endregion

        #region Collections
        public NeutronSafeDictionary<int, NeutronPlayer> Players = new NeutronSafeDictionary<int, NeutronPlayer>();
        #endregion

        #region Variables
        protected IPEndPoint _udpEndPoint;
        #endregion

        #region Threading
        protected CancellationTokenSource TokenSource = new CancellationTokenSource();
        #endregion

        public void Initialize()
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

            TcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Tcp)));
            UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));

#if UNITY_EDITOR
            Application.quitting += OnQuit;
#endif
        }

        public void Dispose()
        {
            TokenSource.Cancel();
            TcpClient.Dispose();
            UdpClient.Dispose();
        }

        public void OnQuit() => Dispose();
    }
}
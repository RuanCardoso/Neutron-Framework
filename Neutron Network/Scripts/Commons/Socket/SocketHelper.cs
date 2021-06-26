using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NeutronNetwork;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Server;
using NeutronNetwork.Server.Internal;
using UnityEngine;

namespace NeutronNetwork.Helpers
{
    public static class SocketHelper
    {
        public static bool GetPlayer(TcpClient nSocket, out Player nPlayer)
        {
            return Neutron.Server.PlayersBySocket.TryGetValue(nSocket, out nPlayer);
        }

        public static bool AddPlayer(Player nPlayer)
        {
            return Neutron.Server.PlayersBySocket.TryAdd(nPlayer.tcpClient, nPlayer)
                && MatchmakingHelper.AddPlayer(nPlayer);
        }

        public static bool RemovePlayerFromServer(Player nPlayer)
        {
            bool tryRemove = Neutron.Server.PlayersBySocket.TryRemove(nPlayer.tcpClient, out Player __)
                && Neutron.Server.PlayersById.TryRemove(nPlayer.ID, out Player _);
            if (tryRemove)
            {
                #region Provider
                Neutron.Server.generatedIds.Enqueue(nPlayer.ID);
                #endregion
                string addr = nPlayer.RemoteEndPoint().Address.ToString();
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                    Neutron.Server.RegisteredConnectionsByIp[addr] = --value;
                MatchmakingHelper.DestroyPlayer(nPlayer);
                PlayerHelper.Disconnect(nPlayer, "Exited");
                if (nPlayer.IsInRoom())
                {
                    INeutronMatchmaking matchmaking = nPlayer.Matchmaking;
                    if (matchmaking != null)
                    {
                        if (matchmaking.RemovePlayer(nPlayer))
                            MatchmakingHelper.Leave(nPlayer, leaveChannel: false);
                        if (nPlayer.IsInChannel())
                        {
                            matchmaking = nPlayer.Matchmaking;
                            if (matchmaking.RemovePlayer(nPlayer))
                                MatchmakingHelper.Leave(nPlayer, leaveRoom: false);
                        }
                    }
                    else NeutronLogger.Print("Matchmaking not found!");
                }
                else
                {
                    INeutronMatchmaking matchmaking = nPlayer.Matchmaking;
                    if (matchmaking != null)
                    {
                        if (matchmaking.RemovePlayer(nPlayer))
                            MatchmakingHelper.Leave(nPlayer, leaveRoom: false);
                    }
                }
                Neutron.Server.CurrentPlayers--;
            }
            return tryRemove;
        }

        public static async Task<bool> ReadAsyncBytes(Stream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                int bytesRead = 0;
                try
                {
                    while (count > 0)
                    {
                        if ((bytesRead = await stream.ReadAsync(buffer, offset, count, token)) > 0)
                        {
                            offset += bytesRead;
                            count -= bytesRead;
                        }
                        else return false;
                    }
                    return count <= 0;
                }
                catch { return false; }
            });
        }

        //The Server cannot transmit all data to nothing.
        public static void Redirect(Player nSender, Protocol nProtocol, SendTo nSendTo, byte[] nBuffer, Player[] nPlayers)
        {
            DataBuffer dataBuffer = new DataBuffer(nBuffer, nProtocol);
            if (nSendTo == SendTo.Me)
                if (!nSender.IsServer)
                    nSender.qData.Add(dataBuffer, nSender._cts.Token);
                else NeutronLogger.Print("The Server cannot transmit data to itself.");
            else
            {
                if (nPlayers != null)
                {
                    for (int i = 0; i < nPlayers.Length; i++)
                    {
                        switch (nSendTo)
                        {
                            case SendTo.All:
                                nPlayers[i].qData.Add(dataBuffer, nPlayers[i]._cts.Token);
                                break;
                            case SendTo.Others:
                                if (nPlayers[i].Equals(nSender))
                                    continue;
                                else
                                    nPlayers[i].qData.Add(dataBuffer, nPlayers[i]._cts.Token);
                                break;
                        }
                    }
                }
            }
        }

        public static bool LimitConnectionsByIP(TcpClient Socket)
        {
            string addr = ((IPEndPoint)Socket.Client.RemoteEndPoint).Address.ToString();
            if (addr != IPAddress.Loopback.ToString())
            {
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int count))
                {
                    if (count > NeutronServer.LIMIT_OF_CONNECTIONS_BY_IP)
                        return false;
                    else
                    {
                        Neutron.Server.RegisteredConnectionsByIp[addr] = count + 1;
                        {
                            return true;
                        }
                    }
                }
                else return Neutron.Server.RegisteredConnectionsByIp.TryAdd(addr, 1);
            }
            else return true;
        }

        public static bool IsConnected(this TcpClient socket)
        {
            try
            {
                return !(socket.Client.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        public static int GetFreePort(Protocol type)
        {
            switch (type)
            {
                case Protocol.Udp:
                    {
                        UdpClient freePort = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                        IPEndPoint endPoint = (IPEndPoint)freePort.Client.LocalEndPoint;
                        int port = endPoint.Port;
                        freePort.Close();
                        return port;
                    }
                case Protocol.Tcp:
                    {
                        TcpClient freePort = new TcpClient(new IPEndPoint(IPAddress.Any, 0));
                        IPEndPoint endPoint = (IPEndPoint)freePort.Client.LocalEndPoint;
                        int port = endPoint.Port;
                        freePort.Close();
                        return port;
                    }
                default:
                    return 0;
            }
        }

        public static IPEndPoint RemoteEndPoint(this Player socket)
        {
            return (IPEndPoint)socket.tcpClient.Client.RemoteEndPoint;
        }

        public static async Task<IPAddress> GetHostAddress(string host)
        {
            return (await Dns.GetHostAddressesAsync(host))[0];
        }

        public static void Dispose()
        {
            var l_Players = Neutron.Server.PlayersBySocket.Values.ToList();
            foreach (var p_Player in l_Players)
                p_Player.Dispose();
            Neutron.Server.TcpSocket.Stop();
        }
    }
}
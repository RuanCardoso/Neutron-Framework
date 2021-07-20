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
        public static bool GetPlayer(TcpClient nSocket, out NeutronPlayer nPlayer)
        {
            return Neutron.Server.PlayersBySocket.TryGetValue(nSocket, out nPlayer);
        }

        public static bool AddPlayer(NeutronPlayer nPlayer)
        {
            return Neutron.Server.PlayersBySocket.TryAdd(nPlayer.m_TcpClient, nPlayer)
                && MatchmakingHelper.AddPlayer(nPlayer);
        }

        public static bool RemovePlayerFromServer(NeutronPlayer nPlayer)
        {
            bool tryRemove = Neutron.Server.PlayersBySocket.TryRemove(nPlayer.m_TcpClient, out NeutronPlayer __)
                && Neutron.Server.PlayersById.TryRemove(nPlayer.ID, out NeutronPlayer _);
            if (tryRemove)
            {
                Neutron.Server.m_PooledIds.Enqueue(nPlayer.ID);
                if (nPlayer.rPEndPoint != null)
                {
                    string addr = nPlayer.rPEndPoint.Address.ToString();
                    if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                        Neutron.Server.RegisteredConnectionsByIp[addr] = --value;
                }

                PlayerHelper.Disconnect(nPlayer, "Exited");
                //MatchmakingHelper.DestroyPlayer(nPlayer);

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
                    else LogHelper.Error("Matchmaking not found!");
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
                Interlocked.Decrement(ref Neutron.Server.CurrentPlayers);
            }
            return tryRemove;
        }

        public static async Task<bool> ReadAsyncBytes(NetworkStream stream, byte[] buffer, int offset, int size, CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    int bytesRead;
                    while (offset < size) //* Por causa desse while eu executo em uma Task separada, pra não causar atraso no Thread de recebimento, assim interferindo no UDP.
                    {
                        int bytesRemaining = size - offset;
                        if ((bytesRead = await stream.ReadAsync(buffer, offset, bytesRemaining, token)) > 0)
                            offset += bytesRead;
                        else
                            return false;
                    }
                    return offset == size;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }
            });
        }

        //The Server cannot transmit all data to nothing.
        public static void Redirect(NeutronPlayer nSender, Protocol nProtocol, TargetTo nSendTo, byte[] nBuffer, NeutronPlayer[] nPlayers)
        {
            NeutronData dataBuffer = new NeutronData(nBuffer, nProtocol);
            if (nSendTo == TargetTo.Me)
                if (!nSender.IsServer)
                    Neutron.Server.OnSendingData(nSender, dataBuffer);
                else LogHelper.Error("The Server cannot transmit data to itself.");
            else
            {
                if (nPlayers != null)
                {
                    for (int i = 0; i < nPlayers.Length; i++)
                    {
                        switch (nSendTo)
                        {
                            case TargetTo.All:
                                Neutron.Server.OnSendingData(nPlayers[i], dataBuffer);
                                break;
                            case TargetTo.Others:
                                if (nPlayers[i].Equals(nSender))
                                    continue;
                                else
                                    Neutron.Server.OnSendingData(nPlayers[i], dataBuffer);
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

        public static async Task<IPAddress> GetHostAddress(string host)
        {
            return (await Dns.GetHostAddressesAsync(host))[0];
        }
    }
}
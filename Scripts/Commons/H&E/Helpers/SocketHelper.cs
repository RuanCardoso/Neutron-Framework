using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NeutronNetwork.Helpers
{
    public static class SocketHelper
    {
        public static bool GetPlayer(TcpClient client, out NeutronPlayer player)
        {
            return Neutron.Server.PlayersBySocket.TryGetValue(client, out player);
        }

        public static bool AddPlayer(NeutronPlayer player)
        {
            return Neutron.Server.PlayersBySocket.TryAdd(player.TcpClient, player)
                && MatchmakingHelper.AddPlayer(player);
        }

        public static bool RemovePlayerFromServer(NeutronPlayer player)
        {
            bool tryRemove = Neutron.Server.PlayersBySocket.TryRemove(player.TcpClient, out NeutronPlayer __)
                && Neutron.Server.PlayersById.TryRemove(player.ID, out NeutronPlayer _);
            if (tryRemove)
            {
                Neutron.Server._pooledIds.Enqueue(player.ID);
                if (player.RemoteEndPoint != null)
                {
                    string addr = player.RemoteEndPoint.Address.ToString();
                    if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                        Neutron.Server.RegisteredConnectionsByIp[addr] = --value;
                }

                PlayerHelper.Disconnect(player, "Exited");
                //MatchmakingHelper.DestroyPlayer(nPlayer);

                if (player.IsInRoom())
                {
                    INeutronMatchmaking matchmaking = player.Matchmaking;
                    if (matchmaking != null)
                    {
                        if (matchmaking.Remove(player))
                            MatchmakingHelper.Leave(player, leaveChannel: false);
                        if (player.IsInChannel())
                        {
                            matchmaking = player.Matchmaking;
                            if (matchmaking.Remove(player))
                                MatchmakingHelper.Leave(player, leaveRoom: false);
                        }
                    }
                    else
                        LogHelper.Error("Matchmaking not found!");
                }
                else
                {
                    INeutronMatchmaking matchmaking = player.Matchmaking;
                    if (matchmaking != null)
                    {
                        if (matchmaking.Remove(player))
                            MatchmakingHelper.Leave(player, leaveRoom: false);
                    }
                }
                Interlocked.Decrement(ref Neutron.Server.PlayerCount);
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

        public static void Redirect(NeutronPlayer player, Protocol protocol, TargetTo targetTo, byte[] buffer, NeutronPlayer[] players)
        {
            NeutronData dataBuffer = new NeutronData(buffer, protocol);
            if (targetTo == TargetTo.Me)
                if (!player.IsServer)
                    Neutron.Server.OnSendingData(player, dataBuffer);
                else
                    LogHelper.Error("The Server cannot transmit data to itself.");
            else
            {
                if (players != null)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        switch (targetTo)
                        {
                            case TargetTo.All:
                                Neutron.Server.OnSendingData(players[i], dataBuffer);
                                break;
                            case TargetTo.Others:
                                if (players[i].Equals(player))
                                    continue;
                                else
                                    Neutron.Server.OnSendingData(players[i], dataBuffer);
                                break;
                        }
                    }
                }
                else
                    LogHelper.Error("The Server cannot transmit all data to nothing.");
            }
        }

        public static bool AllowConnection(TcpClient client)
        {
            string addr = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            if (addr != IPAddress.Loopback.ToString())
            {
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int count))
                {
                    if (count > NeutronMain.Settings.LIMIT_OF_CONN_BY_IP)
                        return false;
                    else
                    {
                        Neutron.Server.RegisteredConnectionsByIp[addr] = count + 1;
                        {
                            return true;
                        }
                    }
                }
                else
                    return Neutron.Server.RegisteredConnectionsByIp.TryAdd(addr, 1);
            }
            else
                return true;
        }

        public static bool Poll(this TcpClient client)
        {
            try
            {
                return !(client.Client.Poll(1, SelectMode.SelectRead) && client.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        public static int GetFreePort(Protocol protocol)
        {
            switch (protocol)
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
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
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
                && MatchmakingHelper.Internal.AddPlayer(player);
        }

        public static bool RemovePlayerFromServer(NeutronPlayer player)
        {
            bool tryRemove = Neutron.Server.PlayersBySocket.TryRemove(player.TcpClient, out NeutronPlayer __)
                && Neutron.Server.PlayersById.TryRemove(player.ID, out NeutronPlayer _);
            if (tryRemove)
            {
                Neutron.Server._pooledIds.Enqueue(player.ID);
                string addr = player.StateObject.TcpRemoteEndPoint.Address.ToString();
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                    Neutron.Server.RegisteredConnectionsByIp[addr] = --value;
                PlayerHelper.Disconnect(player, "Exited");
                //MatchmakingHelper.DestroyPlayer(nPlayer);
                if (player.IsInRoom())
                {
                    INeutronMatchmaking matchmaking = player.Matchmaking;
                    if (matchmaking != null)
                    {
                        if (matchmaking.Remove(player))
                            MatchmakingHelper.Internal.Leave(player, leaveChannel: false);
                        if (player.IsInChannel())
                        {
                            matchmaking = player.Matchmaking;
                            if (matchmaking.Remove(player))
                                MatchmakingHelper.Internal.Leave(player, leaveRoom: false);
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
                            MatchmakingHelper.Internal.Leave(player, leaveRoom: false);
                    }
                }
                Interlocked.Decrement(ref Neutron.Server.PlayerCount);
            }
            return tryRemove;
        }

        public static Task<bool> ReadAsyncBytes(Stream stream, byte[] buffer, int offset, int size, CancellationToken token) // Manter Stream, em vez de NetworkStream.
        {
            return Task.Run(async () =>
            {
                try
                {
                    int bytesRead;
                    while (offset < size) //* Execute em uma task separada, evita problemas com o UDP.
                    {
                        int bytesRemaining = size - offset;
                        if ((bytesRead = await stream.ReadAsync(buffer, offset, bytesRemaining, token)) > 0)
                            offset += bytesRead;
                        else
                            return false;
                    }
                    return offset == size;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        public static IAsyncResult BeginReadBytes(UdpClient udpClient, StateObject stateObject, AsyncCallback callback)
        {
            Socket udpSocket = udpClient.Client;
            return udpSocket.BeginReceiveFrom(stateObject.Buffer, 0, StateObject.Size, SocketFlags.None, ref stateObject.NonAllocEndPoint, callback, null);
        }

        public static int EndReadBytes(UdpClient udpClient, ref EndPoint remoteEp, IAsyncResult ar)
        {
            Socket udpSocket = udpClient.Client;
            return udpSocket.EndReceiveFrom(ar, ref remoteEp);
        }

        public static Task<bool> ReadAsyncBytes(UdpClient udpClient, StateObject stateObject)
        {
            Socket udpSocket = udpClient.Client;
            //if (stateObject.ReadAsyncBytes == null)
            {
                stateObject.ReadAsyncBytes = Task.Factory.FromAsync((callback, obj) => udpSocket.BeginReceiveFrom(stateObject.Buffer, 0, StateObject.Size, SocketFlags.None, ref stateObject.NonAllocEndPoint, callback, obj), (ar) =>
                {
                    try
                    {
                        EndPoint remoteEp = stateObject.NonAllocEndPoint;
                        int bytesRead = udpSocket.EndReceiveFrom(ar, ref remoteEp);
                        //* Esta regi�o funciona como um "Syn/Ack", o cliente envia algum pacote vazio ap�s a conex�o, ap�s o servidor receber este pacote, atribui o ip de destino, que � para onde os dados ser�o enviados.
                        //! Se o ip de destino � nulo, o servidor n�o enviar� os dados, porque n�o tem destino, n�o houve "Syn/Ack".
                        //! A tentativa de envio sem o "Syn/Ack" causar� a exce��o de "An existing connection was forcibly closed by the remote host"
                        if (!stateObject.UdpIsReady())
                            stateObject.UdpRemoteEndPoint = (IPEndPoint)remoteEp;
                        if (bytesRead > 0)
                        {
                            stateObject.ReceivedDatagram = new byte[bytesRead];
                            //**************************************************************************
                            Buffer.BlockCopy(stateObject.Buffer, 0, stateObject.ReceivedDatagram, 0, bytesRead);
                        }
                        return bytesRead > 0;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }, null);
            }
            return stateObject.ReadAsyncBytes;
        }

        public static Task<int> SendAsyncBytes(UdpClient udpClient, StateObject stateObject, IPEndPoint remoteEp)
        {
            Socket udpSocket = udpClient.Client;
            //if (stateObject.SendAsyncBytes == null)
            {
                stateObject.SendAsyncBytes = Task.Factory.FromAsync((callback, obj) => udpSocket.BeginSendTo(stateObject.SendDatagram, 0, stateObject.SendDatagram.Length, SocketFlags.None, remoteEp, callback, obj), (ar) =>
                {
                    return udpSocket.EndSendTo(ar);
                }, null);
            }
            return stateObject.SendAsyncBytes;
        }

        public static void BeginSendBytes(UdpClient udpClient, byte[] datagram, IPEndPoint remoteEp, AsyncCallback callback)
        {
            Socket udpSocket = udpClient.Client;
            udpSocket.BeginSendTo(datagram, 0, datagram.Length, SocketFlags.None, remoteEp, callback, null);
        }

        public static int EndSendBytes(UdpClient udpClient, IAsyncResult ar)
        {
            Socket udpSocket = udpClient.Client;
            return udpSocket.EndSendTo(ar);
        }

        public static int SendBytes(UdpClient udpClient, byte[] datagram, IPEndPoint remoteEp)
        {
            Socket udpSocket = udpClient.Client;
            return udpSocket.SendTo(datagram, 0, datagram.Length, SocketFlags.None, remoteEp);
        }

        //* Envia os dados de forma ass�ncrona no socket UDP, algu�m sabe como melhorar isso? faz muitas aloca��es de GC, e usa muita CPU, por causa do "Task.Factory.FromAsync", melhor usar o s�ncrono ou beginreceive diretamente.
        public static async void SendUdpAsync(UdpClient udpClient, StateObject stateObject, IPEndPoint iPEndPoint)
        {
            await SendAsyncBytes(udpClient, stateObject, iPEndPoint);
        }

        //* Escreve no socket de modo ass�ncrono no socket TCP.
        public static async void SendTcpAsync(NetworkStream networkStream, byte[] buffer, CancellationToken token)
        {
            await networkStream.WriteAsync(buffer, 0, buffer.Length, token);
        }

        public static void Redirect(NeutronPacket packet, TargetTo targetTo, NeutronPlayer[] players)
        {
            switch (targetTo)
            {
                case TargetTo.Me:
                    if (!packet.Owner.IsServer)
                        Neutron.Server.OnSendingData(packet.Owner, packet);
                    else
                        LogHelper.Error("The Server cannot transmit data to itself.");
                    break;
                case TargetTo.Server:
                    break;
                default:
                    {
                        if (players != null)
                        {
                            for (int i = 0; i < players.Length; i++)
                            {
                                switch (targetTo)
                                {
                                    case TargetTo.All:
                                        Neutron.Server.OnSendingData(players[i], packet);
                                        break;
                                    case TargetTo.Others:
                                        if (players[i].Equals(packet.Owner))
                                            continue;
                                        Neutron.Server.OnSendingData(players[i], packet);
                                        break;
                                }
                            }
                        }
                        else
                            LogHelper.Error("The server cannot transmit all data to nothing.");
                        break;
                    }
            }
            packet.Recycle();
        }

        public static void Redirect(NeutronPlayer owner, NeutronPlayer sender, Protocol protocol, TargetTo targetTo, Packet ignoredPacket, byte[] buffer, NeutronPlayer[] players)
        {
            NeutronPacket packet = new NeutronPacket(buffer, owner, sender, protocol, ignoredPacket);
            switch (targetTo)
            {
                case TargetTo.Me:
                    if (!owner.IsServer)
                        Neutron.Server.OnSendingData(owner, packet);
                    else
                        LogHelper.Error("The Server cannot transmit data to itself.");
                    break;
                case TargetTo.Server:
                    break;
                default:
                    {
                        if (players != null)
                        {
                            for (int i = 0; i < players.Length; i++)
                            {
                                switch (targetTo)
                                {
                                    case TargetTo.All:
                                        Neutron.Server.OnSendingData(players[i], packet);
                                        break;
                                    case TargetTo.Others:
                                        if (players[i].Equals(owner))
                                            continue;
                                        Neutron.Server.OnSendingData(players[i], packet);
                                        break;
                                }
                            }
                        }
                        else
                            LogHelper.Error("The server cannot transmit all data to nothing.");
                        break;
                    }
            }
        }

        public static bool AllowConnection(TcpClient client)
        {
            string addr = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            if (addr != IPAddress.Loopback.ToString())
            {
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int count))
                {
                    if (count > OthersHelper.GetConstants().MaxConnectionsPerIp)
                        return false;
                    else
                    {
                        Neutron.Server.RegisteredConnectionsByIp[addr] = count + 1;
                        return true;
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

        public static Stream GetStream(TcpClient tcpClient)
        {
            Stream networkStream = tcpClient.GetStream();
            if (OthersHelper.GetConstants().BufferedStream)
                return new BufferedStream(networkStream, OthersHelper.GetConstants().BufferedStreamSize);
            else
                return networkStream;
        }
    }
}
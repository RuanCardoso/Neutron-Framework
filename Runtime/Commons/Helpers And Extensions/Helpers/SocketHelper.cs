using NeutronNetwork.Extensions;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
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
        #region Commons
        public static bool GetPlayer(TcpClient client, out NeutronPlayer player)
        {
            return Neutron.Server.PlayersBySocket.TryGetValue(client, out player);
        }

        public static bool AddPlayer(NeutronPlayer player)
        {
            return Neutron.Server.PlayersBySocket.TryAdd(player.TcpClient, player)
                && MatchmakingHelper.Internal.AddPlayer(player);
        }

        //* sabe deus.
        public static bool RemovePlayerFromServer(NeutronPlayer player)
        {
            bool tryRemove = Neutron.Server.PlayersBySocket.TryRemove(player.TcpClient, out NeutronPlayer __)
                && Neutron.Server.PlayersById.TryRemove(player.Id, out NeutronPlayer _);
            if (tryRemove)
            {
                Neutron.Server._pooledIds.Push(player.Id);
                string addr = player.StateObject.TcpRemoteEndPoint.Address.ToString();
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                    Neutron.Server.RegisteredConnectionsByIp[addr] = --value;
                MatchmakingHelper.Destroy(player);
                PlayerHelper.Disconnect(player, "Exited");
                if (player.IsInRoom())
                {
                    INeutronMatchmaking matchmaking = player.Matchmaking;
                    if (matchmaking != null)
                    {
                        //* Sai da sala.
                        if (matchmaking.Remove(player))
                            MatchmakingHelper.Internal.Leave(player, MatchmakingMode.Room);
                        //* Sai do canal.
                        if (player.IsInChannel())
                        {
                            matchmaking = player.Matchmaking;
                            if (matchmaking.Remove(player))
                                MatchmakingHelper.Internal.Leave(player, MatchmakingMode.Channel);
                        }
                    }
                    else
                        LogHelper.Error("Leave: Matchmaking not found!");
                }
                else
                {
                    //* Sai do canal.
                    INeutronMatchmaking matchmaking = player.Matchmaking;
                    if (matchmaking != null)
                    {
                        if (matchmaking.Remove(player))
                            MatchmakingHelper.Internal.Leave(player, MatchmakingMode.Channel);
                    }
                }
                Interlocked.Decrement(ref Neutron.Server._playerCount);
            }
            else
                LogHelper.Error("Failed to remove player from server!");
            return tryRemove;
        }
        #endregion

        #region Tcp
        //* Ler do fluxo tcp.
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

        //* Escreve no socket de modo assíncrono no socket TCP.
        public static async void SendTcpAsync(NetworkStream networkStream, byte[] buffer, CancellationToken token)
        {
            await networkStream.WriteAsync(buffer, 0, buffer.Length, token);
        }
        #endregion

        #region Udp
        //* ler dados udp.
        public static IAsyncResult BeginReadBytes(UdpClient udpClient, StateObject stateObject, AsyncCallback callback)
        {
            Socket udpSocket = udpClient.Client;
            return udpSocket.BeginReceiveFrom(stateObject.ReceivedDatagram, 0, StateObject.Size, SocketFlags.None, ref stateObject.NonAllocEndPoint, callback, null);
        }

        public static int EndReadBytes(UdpClient udpClient, ref EndPoint remoteEp, IAsyncResult ar)
        {
            Socket udpSocket = udpClient.Client;
            return udpSocket.EndReceiveFrom(ar, ref remoteEp);
        }

        //* ler dados udp.
        public static Task<bool> ReadAsyncBytes(UdpClient udpClient, StateObject stateObject)
        {
            Socket udpSocket = udpClient.Client;
            var task = Task.Factory.FromAsync((callback, obj) => udpSocket.BeginReceiveFrom(stateObject.ReceivedDatagram, 0, StateObject.Size, SocketFlags.None, ref stateObject.NonAllocEndPoint, callback, obj), (ar) =>
            {
                try
                {
                    EndPoint remoteEp = stateObject.NonAllocEndPoint;
                    int bytesRead = udpSocket.EndReceiveFrom(ar, ref remoteEp);
                    //* Esta região funciona como um "Syn/Ack", o cliente envia algum pacote vazio após a conexão, após o servidor receber este pacote, atribui o ip de destino, que é para onde os dados serão enviados.
                    //! Se o ip de destino é nulo, o servidor não enviará os dados, porque não tem destino, não houve "Syn/Ack".
                    //! A tentativa de envio sem o "Syn/Ack" causará a exceção de "An existing connection was forcibly closed by the remote host"
                    if (!stateObject.UdpIsReady())
                        stateObject.UdpRemoteEndPoint = (IPEndPoint)remoteEp;
                    if (bytesRead > 0)
                    {
                        stateObject.SlicedDatagram = new byte[bytesRead];
                        Buffer.BlockCopy(stateObject.ReceivedDatagram, 0, stateObject.SlicedDatagram, 0, bytesRead);
                    }
                    return bytesRead > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }, null);
            return task;
        }

        public static Task<int> SendAsyncBytes(UdpClient udpClient, StateObject stateObject, IPEndPoint remoteEp)
        {
            Socket udpSocket = udpClient.Client;
            var task = Task.Factory.FromAsync((callback, obj) => udpSocket.BeginSendTo(stateObject.SendDatagram, 0, stateObject.SendDatagram.Length, SocketFlags.None, remoteEp, callback, obj), (ar) =>
            {
                return udpSocket.EndSendTo(ar);
            }, null);
            return task;
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

        //* Envia os dados de forma assíncrona no socket UDP, alguém sabe como melhorar isso? faz muitas alocações de GC, e usa muita CPU, por causa do "Task.Factory.FromAsync", melhor usar o síncrono ou beginreceive diretamente.
        public static async void SendUdpAsync(UdpClient udpClient, StateObject stateObject, IPEndPoint iPEndPoint)
        {
            await SendAsyncBytes(udpClient, stateObject, iPEndPoint);
        }
        #endregion

        #region Others
        public static bool AllowConnection(TcpClient client)
        {
            string addr = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            if (addr != IPAddress.Loopback.ToString())
            {
                if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int count))
                {
                    if (count > Helper.GetConstants().MaxConnectionsPerIp)
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
            catch (SocketException)
            {
                return false;
            }
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

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static Stream GetStream(TcpClient tcpClient)
        {
            Stream networkStream = tcpClient.GetStream();
            if (Helper.GetConstants().Tcp.BufferedStream)
                return new BufferedStream(networkStream, Helper.GetConstants().Tcp.BufferedStreamSize);
            else
                return networkStream;
        }
        #endregion
    }
}
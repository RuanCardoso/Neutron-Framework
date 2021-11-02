using Asyncoroutine;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Packets;
using NeutronNetwork.UI;
using NeutronNetwork.Wrappers;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork.Server
{
    /// <summary>
    ///* This class is the server class, which is responsible for receiving and sending packets to the client.
    /// </summary>
    [RequireComponent(typeof(NeutronModule))]
    [RequireComponent(typeof(NeutronSchedule))]
    [RequireComponent(typeof(NeutronFramerate))]
    [RequireComponent(typeof(NeutronStatistics))]
    [RequireComponent(typeof(NeutronUI))]
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_SERVER)]
    public class NeutronServer : ServerBase
    {
        //* clumsy filters to simulate lag.
        public static StringBuilder filter_tcp_udp_client_server = new StringBuilder();
        public static StringBuilder filter_udp_client_server = new StringBuilder();
        public static StringBuilder filter_tcp_client_server = new StringBuilder();
        public static StringBuilder filter_tcp_client = new StringBuilder();
        public static StringBuilder filter_tcp_server = new StringBuilder();
        public static StringBuilder filter_udp_client = new StringBuilder();
        public static StringBuilder filter_udp_server = new StringBuilder();

        #region Events
        /// <summary>
        ///* This event is triggered when a server is started.
        /// </summary>
        public static event NeutronEventNoReturn OnStart;
        /// <summary>
        ///* This event is triggered when the server receives a packet.
        /// </summary>
        public static event NeutronEventWithReturn<Packet, bool> OnReceivePacket;
        #endregion

        #region Properties
        /// <summary>
        ///* Returns is server is running.
        /// </summary>
        public static bool Initialized
        {
            get;
            set;
        }

        /// <summary>
        ///* Ref player.
        /// </summary>
        public NeutronPlayer Player
        {
            get;
            set;
        }

        /// <summary>
        ///* Ref neutron instance.
        /// </summary>
        public Neutron Instance
        {
            get;
            set;
        }

        public bool IsUnityThread { get; set; }
        #endregion

        #region Fields -> Collections
        /// <summary>
        ///* Store all accepted clients and processes them.
        /// </summary>
        private INeutronConsumer<TcpClient> _acceptedClients;
        /// <summary>
        ///* Store all packets and processes them.
        /// </summary>
        private INeutronConsumer<NeutronPacket> _dataForProcessing;
        //* Store all player ids.
        //* When a player disconnects, the id is added to this list.
        public NeutronSafeQueueNonAlloc<int> _pooledIds = new NeutronSafeQueueNonAlloc<int>();
        #endregion

        #region Threading
        //* Used to stop all server threads.
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        #endregion

        #region Functions
        private void StartThreads()
        {
            #region Initialize Collections
            _acceptedClients = !IsUnityThread ? (INeutronConsumer<TcpClient>)new NeutronBlockingQueue<TcpClient>() : (INeutronConsumer<TcpClient>)new NeutronSafeQueueNonAlloc<TcpClient>();
            _dataForProcessing = !IsUnityThread ? (INeutronConsumer<NeutronPacket>)new NeutronBlockingQueue<NeutronPacket>() : (INeutronConsumer<NeutronPacket>)new NeutronSafeQueueNonAlloc<NeutronPacket>();
            #endregion

            Player = PlayerHelper.MakeTheServerPlayer(); //* Create the ref player.

            Instance = new Neutron(Player, true); //* Create the ref instance.
            Instance.Initialize(Instance); //* Initialize the instance.

            #region Provider
            for (int i = 0; i < NeutronModule.Settings.GlobalSettings.MaxPlayers; i++)
                _pooledIds.Push((NeutronConstants.GENERATE_PLAYER_ID + i) + 1); //* Add all player ids to the list.
            #endregion

            Initialized = true; //* Mark the server as initialized.

            #region Logger
#if NET_STANDARD_2_1
           //*
#endif
            LogHelper.Info("The server is ready, all protocols(TCP, UDP, RUDP) have been initialized.\r\n");
            LogHelper.Info($"Server address: {SocketHelper.GetLocalIPAddress()}\r\n");
            #endregion

            #region Threads
            CancellationToken token = TokenSource.Token;
            if (!IsUnityThread)
            {
                Thread acptTh = new Thread(async (t) =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        TcpClient tcpClient = await TcpListener.AcceptTcpClientAsync(); //* wait for new client....
                        if (tcpClient != null)
                            _acceptedClients.Push(tcpClient); //* Add the client to the queue.
                        else
                            LogHelper.Error("Client is null!");
                    }
                })
                {
                    Priority = System.Threading.ThreadPriority.Lowest,
                    IsBackground = true,
                    Name = "Neutron acptTh"
                };
                acptTh.Start();
            }
            else
                StartCoroutine(OnAcceptedClientCouroutine(token));

            if (!IsUnityThread)
            {
                Thread packetProcessingStackTh = new Thread((e) =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        PacketProcessingStack();
                    }
                })
                {
                    Priority = System.Threading.ThreadPriority.Normal,
                    IsBackground = true,
                    Name = "Neutron packetProcessingStackTh"
                };
                packetProcessingStackTh.Start();
            }

            if (!IsUnityThread)
            {
                Thread clientsProcessingStackTh = new Thread((e) =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        ClientsProcessingStack();
                    }
                })
                {
                    Priority = System.Threading.ThreadPriority.Lowest,
                    IsBackground = true,
                    Name = "Neutron ClientsProcessingStackTh"
                };
                clientsProcessingStackTh.Start();
            }

            if (IsUnityThread)
            {
                StartCoroutine(OnReceivingDataCoroutine(token, Protocol.Tcp));
                StartCoroutine(OnReceivingDataCoroutine(token, Protocol.Udp));
            }
            #endregion

            #region Events
            OnStart?.Invoke();
            #endregion
        }

        private IEnumerator OnAcceptedClientCouroutine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var tcpClientCoroutine = TcpListener.AcceptTcpClientAsync().AsCoroutine(); //* Accept the client.
                yield return tcpClientCoroutine;
                _acceptedClients.Push(tcpClientCoroutine.Result); //* Add the client to the queue.
            }
        }

        private void ClientsProcessingStack()
        {
            CancellationToken token = TokenSource.Token;
            TcpClient tcpClient = _acceptedClients.Pull(); //* Take the client from the queue and block the thread if there is no client.
            if (tcpClient != null)
            {
                if (PlayerHelper.GetAvailableID(out int Id))
                {
                    //* Create the player.
                    if (SocketHelper.AllowConnection(tcpClient))
                    {
                        tcpClient.NoDelay = Helper.GetSettings().GlobalSettings.NoDelay; //* Set the no delay, fast send.
                        tcpClient.ReceiveBufferSize = Helper.GetConstants().Tcp.TcpReceiveBufferSize; //* Set the receive buffer size.
                        tcpClient.SendBufferSize = Helper.GetConstants().Tcp.TcpSendBufferSize; //* Set the send buffer size.
                        var player = new NeutronPlayer(Id, tcpClient, new CancellationTokenSource()); //* Create the player.
                        if (SocketHelper.AddPlayer(player))
                        {
                            Interlocked.Increment(ref _playerCount); //* Increment the player count.

                            #region View
                            NeutronSchedule.ScheduleTask(() =>
                            {
                                GameObject playerGlobalController = GameObject.Instantiate(PlayerGlobalController.gameObject); //* Create the player global controller.
                                playerGlobalController.hideFlags = HideFlags.HideInHierarchy; //* Hide the player global controller.
                                playerGlobalController.name = $"Player Global Controller[{player.Id}]"; //* Set the name of the player global controller.
                                foreach (Component component in playerGlobalController.GetComponents<Component>())
                                {
                                    Type type = component.GetType();
                                    if (type.BaseType != typeof(PlayerGlobalController) && type != typeof(Transform))
                                        GameObject.Destroy(component); //* Destroy all components except the player global controller.
                                    else
                                    {
                                        if (type.BaseType == typeof(PlayerGlobalController))
                                        {
                                            var controller = (PlayerGlobalController)component;
                                            controller.Player = player; //* Set the player.
                                        }
                                    }
                                }
                                SceneHelper.MoveToContainer(playerGlobalController, "Server(Container)"); //* Move the player global controller to the container.
                            });
                            #endregion

                            IPEndPoint tcpRemote = (IPEndPoint)player.TcpClient.Client.RemoteEndPoint; //* Get the remote end point.
                            IPEndPoint tcpLocal = (IPEndPoint)player.TcpClient.Client.LocalEndPoint; //* Get the local end point.
                            IPEndPoint udpLocal = (IPEndPoint)player.UdpClient.Client.LocalEndPoint;
                            //***************************************************************************************************************************************************************************************************************************************************************
                            LogHelper.Info($"\r\nIncoming Client -> Ip: [{tcpRemote.Address.ToString().Bold()}] & Port: [Tcp: {tcpRemote.Port.ToString().Bold()} | Udp: {tcpRemote.Port.ToString().Bold()} - {udpLocal.Port.ToString().Bold()}] -> Id: {Id}\r\n");
#if UNITY_EDITOR
                            if (Helper.GetSettings().ServerSettings.FiltersLog)
                            {
                                LogHelper.Info("\r\nFilters(Server->Client): ".Italic() + $"(tcp.SrcPort == {tcpLocal.Port}) or (udp.SrcPort == {udpLocal.Port})".Bold().Color("yellow") + "\r\n");
                                LogHelper.Info("\r\nFilters(Client->Server): ".Italic() + $"(tcp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {tcpRemote.Port})".Bold().Color("yellow") + "\r\n");
                                LogHelper.Info("\r\nFilters(Client->Server->Client): ".Italic() + $"((tcp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {tcpRemote.Port})) or ((tcp.SrcPort == {tcpLocal.Port}) or (udp.SrcPort == {udpLocal.Port}))".Bold().Color("yellow") + "\r\n");
                            }
                            //***************************************************************************************************************************************************************************************************************************************************************
                            filter_tcp_udp_client_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}((tcp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {tcpRemote.Port})) or ((tcp.SrcPort == {tcpLocal.Port}) or (udp.SrcPort == {udpLocal.Port}))");
                            filter_udp_client_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}((udp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {udpLocal.Port}))");
                            filter_tcp_client_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}((tcp.SrcPort == {tcpRemote.Port}) or (tcp.SrcPort == {tcpLocal.Port}))");
                            filter_tcp_client.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(tcp.SrcPort == {tcpRemote.Port})");
                            filter_tcp_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(tcp.SrcPort == {tcpLocal.Port})");
                            filter_udp_client.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(udp.SrcPort == {tcpRemote.Port})");
                            filter_udp_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(udp.SrcPort == {udpLocal.Port})");
#endif
                            if (!IsUnityThread)
                            {
                                ThreadPool.QueueUserWorkItem((e) =>
                                {
                                    OnReceivingDataLoop(player.NetworkStream, player, Protocol.Tcp);
                                    OnReceivingDataLoop(player.NetworkStream, player, Protocol.Udp);
                                });
                            }
                        }
                        else
                        {
                            if (!LogHelper.Error("Failed to add Player!"))
                                player.Dispose();
                        }
                    }
                    else
                    {
                        if (!LogHelper.Error("Client not allowed!"))
                            tcpClient.Close();
                    }
                }
                else
                {
                    if (!LogHelper.Error("Max players reached!"))
                        tcpClient.Close();
                }
            }
        }

        private void PacketProcessingStack()
        {
            CancellationToken token = TokenSource.Token;
            NeutronPacket packet = _dataForProcessing.Pull(); //* Take the packet from the queue and block the thread until there is a packet.
            if (packet != null)
                RunPacket(packet);
        }

        //* Aqui os dados são enviados aos seus clientes.
        public void OnSendingData(NeutronPlayer player, NeutronPacket neutronPacket)
        {
            try
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    var playerId = (short)neutronPacket.Sender.Id; //* Get the player id.
                    byte[] pBuffer = neutronPacket.Buffer.Compress(); //* Compress the packet.
                    switch (neutronPacket.Protocol)
                    {
                        case Protocol.Tcp:
                            {
                                NeutronStream.IWriter wHeader = stream.Writer;
                                wHeader.WriteByteArrayWithAutoSize(pBuffer); //* Write the packet and its size.
                                wHeader.Write(playerId); //* Write the player id in header.
                                byte[] hBuffer = wHeader.ToArray(); //* Get the header buffer.
                                wHeader.Write(); //* End the header.

                                NetworkStream networkStream = player.TcpClient.GetStream(); //* Get the network stream.
                                switch (Helper.GetConstants().SendModel)
                                {
                                    case SendType.Synchronous:
                                        networkStream.Write(hBuffer, 0, hBuffer.Length); //* Send the header.
                                        break;
                                    default:
                                        if (Helper.GetConstants().SendAsyncPattern == AsynchronousType.APM)
                                            networkStream.Write(hBuffer, 0, hBuffer.Length); //* Send the header.
                                        else
                                            SocketHelper.SendTcpAsync(networkStream, hBuffer, player.TokenSource.Token); //* Send the header.
                                        break;
                                }
                                NeutronStatistics.ServerTCP.AddOutgoing(hBuffer.Length); //* Add the outgoing bytes to the statistics.
                            }
                            break;
                        case Protocol.Udp:
                            {
                                NeutronStream.IWriter wHeader = stream.Writer;
                                wHeader.Write(playerId); //* Write the player id in header.
                                wHeader.WriteNext(pBuffer); //* Write the packet and its size.
                                byte[] hBuffer = wHeader.ToArray(); //* Get the header buffer.
                                wHeader.Write(); //* End the header.

                                player.StateObject.SendDatagram = hBuffer; //* Set the datagram to send.
                                if (player.StateObject.UdpIsReady()) //* Check if the player is ready to send.
                                {
                                    NonAllocEndPoint remoteEp = (NonAllocEndPoint)player.StateObject.NonAllocEndPoint; //* Get the remote end point, prevent GC pressure/allocations.
                                    switch (Helper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            SocketHelper.SendBytes(player.UdpClient, hBuffer, remoteEp); //* Send the datagram.
                                            break;
                                        default:
                                            {
                                                switch (Helper.GetConstants().SendAsyncPattern)
                                                {
                                                    case AsynchronousType.APM:
                                                        {
                                                            SocketHelper.BeginSendBytes(player.UdpClient, hBuffer, remoteEp, (ar) =>
                                                            {
                                                                SocketHelper.EndSendBytes(player.UdpClient, ar); //* End the send.
                                                            }); //* Begin the send.
                                                            break;
                                                        }

                                                    default:
                                                        SocketHelper.SendUdpAsync(player.UdpClient, player.StateObject, remoteEp); //* Send the datagram.
                                                        break;
                                                }
                                                break;
                                            }
                                    }
                                    NeutronStatistics.ServerUDP.AddOutgoing(hBuffer.Length); //* Add the outgoing bytes to the statistics.
                                }
                                else
                                    LogHelper.Error($"{player.StateObject.TcpRemoteEndPoint} Cannot receive UDP data. trying... if you are running on WSL2, change the ip from \"localhost\" to the IP address of WSL2 on the client.");
                            }
                            break;
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        public void AddPacket(NeutronPacket packet)
        {
            lock (Encapsulate.BeginLock)
            {
                if (Encapsulate.Sender != null)
                    packet.Sender = Encapsulate.Sender; //* Set the sender.
                packet.IsServerSide = true; //* Set the packet as server side.
                _dataForProcessing.Push(packet); //* Add the packet to the queue.
            }
        }

        private void CreateUdpPacket(NeutronPlayer player)
        {
            byte[] datagram = player.StateObject.ReceivedDatagram; //* Get the datagram.
            byte[] pBuffer = datagram.Decompress(); //* Decompress the packet.

            NeutronPacket neutronPacket = Helper.PollPacket(pBuffer, player, player, Protocol.Udp); //* Create the packet.
            _dataForProcessing.Push(neutronPacket); //* Add the packet to the queue.

            NeutronStatistics.ServerUDP.AddIncoming(datagram.Length); //* Add the incoming bytes to the statistics.
        }

        private void UdpApmReceive(NeutronPlayer player)
        {
            if (player.TokenSource.Token.IsCancellationRequested)
                return; //* Check if the token is cancelled.

            SocketHelper.BeginReadBytes(player.UdpClient, player.StateObject, (ar) =>
            {
                EndPoint remoteEp = player.StateObject.NonAllocEndPoint; //* Get the remote end point, prevent GC pressure/allocations.
                int bytesRead = SocketHelper.EndReadBytes(player.UdpClient, ref remoteEp, ar); //* End the read.
                if (!player.StateObject.UdpIsReady())
                    player.StateObject.UdpRemoteEndPoint = (IPEndPoint)remoteEp; //* Set the remote end point.
                if (bytesRead > 0)
                {
                    player.StateObject.ReceivedDatagram = new byte[bytesRead]; //* Create the datagram.
                    Buffer.BlockCopy(player.StateObject.Buffer, 0, player.StateObject.ReceivedDatagram, 0, bytesRead); //* Copy the received bytes to the datagram.
                    CreateUdpPacket(player); //* Create the packet.
                }
                if (!IsUnityThread)
                    UdpApmReceive(player); //* Receive again.
            });
        }

        private IEnumerator OnReceivingDataCoroutine(CancellationToken token, Protocol protocol)
        {
            byte[] hBuffer = new byte[NeutronModule.HeaderSize]; //* Create the header buffer.
            while (!token.IsCancellationRequested)
            {
                foreach (NeutronPlayer player in PlayersBySocket.Values)
                {
                    Stream networkStream = player.NetworkStream;
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                var headerTask = SocketHelper.ReadAsyncBytes(networkStream, hBuffer, 0, NeutronModule.HeaderSize, token).AsCoroutine();
                                yield return headerTask;
                                if (headerTask.Result)
                                {
                                    //* Read the header.
                                    int size = ByteHelper.ReadSize(hBuffer); //* Get the packet size.
                                    if (size > Helper.GetConstants().Tcp.MaxTcpPacketSize || size <= 0)
                                    {
                                        //* Check if the packet size is valid.
                                        if (!LogHelper.Error($"Invalid tcp message size! size: {size}"))
                                            DisconnectHandler(player); //* Disconnect the player.
                                    }
                                    else
                                    {
                                        byte[] packetBuffer = new byte[size]; //* Create the packet buffer with the size.
                                        var packetTask = SocketHelper.ReadAsyncBytes(networkStream, packetBuffer, 0, size, token).AsCoroutine();
                                        yield return packetTask;
                                        if (packetTask.Result)
                                        {
                                            //* Read the packet.
                                            packetBuffer = packetBuffer.Decompress(); //* Decompress the packet.
                                            NeutronPacket neutronPacket = Helper.PollPacket(packetBuffer, player, player, Protocol.Tcp); //* Create the packet.

                                            _dataForProcessing.Push(neutronPacket); //* Add the packet to the queue.
                                            NeutronStatistics.ServerTCP.AddIncoming(size + hBuffer.Length); //* Add the incoming bytes to the statistics.
                                        }
                                        else
                                            DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                                    }
                                }
                                else
                                    DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                            }
                            break;
                        case Protocol.Udp:
                            {
                                switch (Helper.GetConstants().ReceiveAsyncPattern)
                                {
                                    case AsynchronousType.TAP:
                                        {
                                            var datagramTask = SocketHelper.ReadAsyncBytes(player.UdpClient, player.StateObject).AsCoroutine();
                                            yield return datagramTask;
                                            if (datagramTask.Result)
                                                CreateUdpPacket(player); //* Create the packet.
                                        }
                                        break;
                                    default:
                                        UdpApmReceive(player); //* Receive the data.
                                        break;
                                }
                            }
                            break;
                    }
                }
                yield return null;
            }
        }

        private async void OnReceivingDataLoop(Stream networkStream, NeutronPlayer player, Protocol protocol)
        {
            CancellationToken token = player.TokenSource.Token;
            try
            {
                bool whileOn = false; //* stop the while loop.
                byte[] hBuffer = new byte[NeutronModule.HeaderSize]; //* Create the header buffer.
                while ((!TokenSource.Token.IsCancellationRequested && !token.IsCancellationRequested) && !whileOn)
                {
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                if (await SocketHelper.ReadAsyncBytes(networkStream, hBuffer, 0, NeutronModule.HeaderSize, token))
                                {
                                    //* Read the header.
                                    int size = ByteHelper.ReadSize(hBuffer); //* Get the packet size.
                                    if (size > Helper.GetConstants().Tcp.MaxTcpPacketSize || size <= 0)
                                    {
                                        //* Check if the packet size is valid.
                                        if (!LogHelper.Error($"Invalid tcp message size! size: {size}"))
                                            DisconnectHandler(player); //* Disconnect the player.
                                    }
                                    else
                                    {
                                        byte[] packetBuffer = new byte[size]; //* Create the packet buffer with the size.
                                        if (await SocketHelper.ReadAsyncBytes(networkStream, packetBuffer, 0, size, token))
                                        {
                                            //* Read the packet.
                                            packetBuffer = packetBuffer.Decompress(); //* Decompress the packet.
                                            NeutronPacket neutronPacket = Helper.PollPacket(packetBuffer, player, player, Protocol.Tcp); //* Create the packet.

                                            _dataForProcessing.Push(neutronPacket); //* Add the packet to the queue.
                                            NeutronStatistics.ServerTCP.AddIncoming(size + hBuffer.Length); //* Add the incoming bytes to the statistics.
                                        }
                                        else
                                            DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                                    }
                                }
                                else
                                    DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                            }
                            break;
                        case Protocol.Udp:
                            {
                                switch (Helper.GetConstants().ReceiveAsyncPattern)
                                {
                                    case AsynchronousType.TAP:
                                        if (await SocketHelper.ReadAsyncBytes(player.UdpClient, player.StateObject))
                                            CreateUdpPacket(player); //* Create the packet.
                                        break;
                                    default:
                                        UdpApmReceive(player); //* Receive the data.
                                        whileOn = true; //* stop the while loop.
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
                if (!token.IsCancellationRequested)
                    DisconnectHandler(player); //* Disconnect the player.
            }
        }
        #endregion

        #region Packets
        private void RunPacket(NeutronPacket neutronPacket)
        {
            byte[] pBuffer = neutronPacket.Buffer; //* Get the packet buffer.
            bool isServer = neutronPacket.IsServerSide; //* Get the packet is server side.
            Protocol protocol = neutronPacket.Protocol; //* Get the packet protocol.
            NeutronPlayer owner = neutronPacket.Owner; //* Get the packet owner.
            NeutronPlayer sender = neutronPacket.Sender; //* Get the packet sender.
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
                reader.SetBuffer(pBuffer); //* Set the buffer.
                Packet outPacket = (Packet)reader.ReadPacket(); //* Read the packet.
                if (OnReceivePacket(outPacket) || isServer)
                {
                    switch (outPacket)
                    {
                        case Packet.TcpKeepAlive:
                            {
                                using (NeutronStream tcpStream = Neutron.PooledNetworkStreams.Pull())
                                {
                                    NeutronStream.IWriter writer = tcpStream.Writer;
                                    writer.WritePacket((byte)Packet.TcpKeepAlive);
                                    owner.Write(writer);
                                }
                            }
                            break;
                        case Packet.Handshake:
                            {
                                var appId = reader.ReadString(); //* Read the app id.
                                var time = reader.ReadDouble(); //* Read the time.
                                var authentication = reader.ReadWithInteger<Authentication>(); //* Read the authentication.
                                if (appId.Decrypt(out appId))
                                {
                                    //* Decrypt the app id.
                                    if (Helper.GetSettings().GlobalSettings.AppId == appId)
                                        HandshakeHandler(owner, time, authentication); //* Handshake the player.
                                    else
                                    {
                                        owner.Error(Packet.Handshake, "Update your game version, it does not match the current server version.");
                                        Task.Run(async () =>
                                        {
                                            await Task.Delay(150); //* Submit the disconnect after receiving the error message.
                                            DisconnectHandler(owner); //* Disconnect the player.
                                        });
                                    }
                                }
                                else if (!LogHelper.Error("Failed to verify handshake!"))
                                    DisconnectHandler(owner); //* Disconnect the player.
                            }
                            break;
                        case Packet.Nickname:
                            {
                                var nickname = reader.ReadString(); //* Read the nickname.
                                SetNicknameHandler(owner, nickname);
                            }
                            break;
                        case Packet.Chat:
                            {
                                var matchmakingTo = default(MatchmakingTo); //* Create the matchmaking to.
                                var viewId = default(int); //* Read the view id.
                                var chatPacket = (ChatMode)reader.ReadPacket(); //* Read the chat mode.
                                switch (chatPacket)
                                {
                                    case ChatMode.Global:
                                        matchmakingTo = (MatchmakingTo)reader.ReadPacket(); //* Read the matchmaking to.
                                        break;
                                    case ChatMode.Private:
                                        viewId = reader.ReadInt(); //* Read the view id.
                                        break;
                                }
                                string message = reader.ReadString();
                                ChatHandler(owner, chatPacket, matchmakingTo, viewId, message);
                            }
                            break;
                        case Packet.iRPC:
                            {
                                var registerType = (RegisterMode)reader.ReadPacket(); //* Read the register mode.
                                var targetTo = (TargetTo)reader.ReadPacket(); //* Read the target to.
                                var cache = (CacheMode)reader.ReadPacket(); //* Read the cache mode.
                                short viewId = reader.ReadShort(); //* Read the view id.
                                var rpcId = reader.ReadByte(); //* Read the rpc id.
                                var instanceId = reader.ReadByte(); //* Read the instance id.
                                var buffer = reader.ReadNext(); //* Read the buffer.
                                iRPCHandler(owner, sender, viewId, rpcId, instanceId, buffer, registerType, targetTo, cache, protocol); //* Handle the iRPC.
                            }
                            break;
                        case Packet.gRPC:
                            {
                                var id = reader.ReadByte(); //* Read the id.
                                var buffer = reader.ReadNext(); //* Read the buffer.
                                gRPCHandler(owner, sender, id, buffer, protocol); //* Handle the gRPC.
                            }
                            break;
                        case Packet.GetChannels:
                            {
                                GetChannelsHandler(owner); //* Handle the get channels.
                            }
                            break;
                        case Packet.JoinChannel:
                            {
                                var channelId = reader.ReadInt(); //* Read the channel id.
                                JoinChannelHandler(owner, channelId); //* Handle the join channel.
                            }
                            break;
                        case Packet.GetCache:
                            {
                                var cachedPacket = (CachedPacket)reader.ReadPacket(); //* Read the cached packet.
                                var Id = reader.ReadByte(); //* Read the id.
                                var includeMe = reader.ReadBool(); //* Send packets to me?
                                GetCacheHandler(owner, cachedPacket, Id, includeMe); //* Handle the get cache.
                            }
                            break;
                        case Packet.CreateRoom:
                            {
                                var password = reader.ReadString(); //* Read the password.
                                var room = reader.ReadWithInteger<NeutronRoom>(); //* Read the room.
                                CreateRoomHandler(owner, room, password); //* Handle the create room.
                            }
                            break;
                        case Packet.GetRooms:
                            {
                                GetRoomsHandler(owner); //* Handle the get rooms.
                            }
                            break;
                        case Packet.JoinRoom:
                            {
                                var roomId = reader.ReadInt(); //* Read the room id.
                                var password = reader.ReadString(); //* Read the password.
                                JoinRoomHandler(owner, roomId, password); //* Handle the join room.
                            }
                            break;
                        case Packet.Leave:
                            {
                                var packet = (MatchmakingMode)reader.ReadPacket(); //* Read the matchmaking mode.
                                if (packet == MatchmakingMode.Room)
                                    LeaveRoomHandler(owner); //* Handle the leave room.
                                else if (packet == MatchmakingMode.Channel)
                                    LeaveChannelHandler(owner); //* Handle the leave channel.
                            }
                            break;
                        case Packet.Destroy:
                            {
                                DestroyPlayerHandler(owner); //* Handle the destroy player.
                            }
                            break;
                        case Packet.SetPlayerProperties:
                            {
                                var properties = reader.ReadString(); //* Read the properties.
                                SetPlayerPropertiesHandler(owner, properties); //* Handle the set player properties.
                            }
                            break;
                        case Packet.SetRoomProperties:
                            {
                                var properties = reader.ReadString(); //* Read the properties.
                                SetRoomPropertiesHandler(owner, properties); //* Handle the set room properties.
                            }
                            break;
                        case Packet.UdpKeepAlive:
                            {
                                var time = reader.ReadDouble(); //* Read the time.
                                PingHandler(owner, time); //* Handle the ping.
                            }
                            break;
                        case Packet.CustomPacket:
                            {
                                // var isMine;
                                // TargetTo targetTo = default(TargetTo);
                                // MatchmakingTo matchmakingTo = default(MatchmakingTo);
                                // int viewId = reader.ReadInt();
                                // byte packet = reader.ReadPacket();
                                // if ((isMine = PlayerHelper.IsMine(owner, viewId)))
                                // {
                                //     targetTo = (TargetTo)reader.ReadPacket();
                                //     matchmakingTo = (MatchmakingTo)reader.ReadPacket();
                                // }
                                // byte[] buffer = reader.ReadWithInteger();
                                // CustomPacketHandler(owner, isMine, viewId, buffer, packet, targetTo, matchmakingTo, protocol);
                            }
                            break;
                        case Packet.AutoSync:
                            {
                                var registerMode = (RegisterMode)reader.ReadPacket(); //* Read the register mode.
                                var viewId = reader.ReadShort(); //* Read the view id.
                                var instanceId = reader.ReadByte(); //* Read the instance id.
                                var parameters = reader.ReadNext(); //* Read the parameters.
                                OnAutoSyncHandler(neutronPacket, viewId, instanceId, parameters, registerMode); //* Handle the auto sync.
                            }
                            break;
                        case Packet.Synchronize:
                            {
                                SynchronizeHandler(owner, protocol); //* Handle the synchronize.
                            }
                            break;
                    }
                }
                else
                    LogHelper.Error("Client is not allowed to run this packet.");
            }
        }
        #endregion

        #region Mono Behaviour
        public void StartServer()
        {
            StartSocket(); //* Start the socket.
            if (IsReady && !AutoStart)
                StartThreads(); //* Start the threads.
        }

#pragma warning disable IDE0051
        private void Start()
#pragma warning restore IDE0051
        {
            IsUnityThread = Helper.GetSettings().GlobalSettings.Performance == ThreadType.Unity;
            if (IsReady && AutoStart)
                StartThreads(); //* Start the threads.
            else if (IsReady && !AutoStart)
            {
#if UNITY_SERVER && !UNITY_EDITOR
                StartThreads(); //* Start the threads.
#endif
            }
        }


#pragma warning disable IDE0051
        private void Update()
#pragma warning restore IDE0051
        {
            if (IsUnityThread)
            {
                ClientsProcessingStack();
                PacketProcessingStack();
            }
        }

#pragma warning disable IDE0051
        private void OnApplicationQuit()
#pragma warning restore IDE0051
        {
            Task.Run(() =>
            {
                TokenSource.Cancel(); //* stop all threads.
                foreach (NeutronPlayer player in PlayersById.Values)
                    player.Dispose(); //* Dispose all players.
                TcpListener.Stop(); //* Stop the server.
                _acceptedClients.Dispose();
                _dataForProcessing.Dispose();
                Initialized = false; //* Set the initialized to false.
            });
        }
        #endregion
    }
}

//* ip addr show eth0 | grep -oP '(?<=inet\s)\d+(\.\d+){3}'
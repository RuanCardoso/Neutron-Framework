using Asyncoroutine;
using NeutronNetwork.Client;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using NeutronNetwork.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* This class is the main class of Neutron.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CLIENT)]
    public class Neutron : ClientBase
    {
        #region Constructors
        /// <summary>
        ///* Constructor of Neutron.
        /// </summary>
        public Neutron() { }
        /// <summary>
        ///* Constructor of Neutron.
        /// </summary>
        /// <param name="player">The player that will be used by Neutron.</param></param>
        /// <param name="isConnected">Indicates if Neutron is already connected.</param></param>
        /// <param name="instance">The instance of Neutron.</param></param>
        public Neutron(NeutronPlayer player, bool isConnected)
        {
            LocalPlayer = player;
            IsConnected = isConnected;
#if UNITY_SERVER && !UNITY_EDITOR
            Client = Server.Instance;
#endif
        }
        #endregion

        #region Static Settings
        /// <summary>
        ///* Get the constants settings of Neutron;
        /// </summary>
        /// <returns></returns>
        private NeutronConstantsSettings Constants => Helper.GetConstants();
        /// <summary>
        ///* Get the settings of Neutron;
        /// </summary>
        /// <returns></returns>
        private Settings Settings => Helper.GetSettings();
        #endregion

        #region Collections
        /// <summary>
        ///* Provides a pool of readers and writers, use it for best performance.
        /// </summary>
        public static NeutronPool<NeutronStream> PooledNetworkStreams
        {
            get;
            set;
        }

        /// <summary>
        ///* Provides a pool of packets, use it for best performance.
        /// </summary>
        internal static NeutronPool<NeutronPacket> PooledNetworkPackets
        {
            get;
            set;
        }

        /// <summary>
        ///* Provides a pool of byte array, use it for best performance.
        /// </summary>
        internal static ArrayPool<byte> PooledByteArray
        {
            get;
        } = ArrayPool<byte>.Create();

        /// <summary>
        ///* This queue will store the packets received from the server to be dequeued and processed, in a single Thread (Thread).
        /// </summary>
        /// <typeparam name="NeutronPacket"></typeparam>
        /// <returns></returns>
        private INeutronConsumer<NeutronPacket> _dataForProcessing;
        #endregion

        #region Fields
        /// <summary>
        ///* The host used to connect.
        /// </summary>
        private string _host;
        #endregion

        #region Properties -> Static
        /// <summary>
        ///* The instance of server.
        /// </summary>
        /// <value></value>
        public static NeutronServer Server
        {
            get => ServerBase.This;
        }
        /// <summary>
        ///* The main instance of client.<br/>
        ///* Returns the server instance if it is a server build. 
        /// </summary>
        /// <value></value>
        public static Neutron Client
        {
            get;
            private set;
        }
        #endregion

        #region Properties -> Instance
        /// <summary>
        ///* The local player of the game.
        /// </summary>
        /// <value></value>
        public NeutronPlayer LocalPlayer
        {
            get;
            private set;
        }

        /// <summary>
        ///* Indicates if Neutron is already connected.
        /// </summary>
        /// <value></value>
        public bool IsConnected
        {
            get;
            private set;
        }

        /// <summary>
        ///* Returns the nickname of the local player.
        /// </summary>
        /// <value></value>
        public string Nickname
        {
            get;
            private set;
        }
        #endregion

        #region Public Events
        /// <summary>
        ///* This event is triggered when the connection is established.
        /// </summary>
        public event NeutronEventNoReturn<bool, Neutron> OnNeutronConnected;
        /// <summary>
        ///* This event is triggered when the authentication returns its state.
        /// </summary>
        public event NeutronEventNoReturn<bool, JObject, Neutron> OnNeutronAuthenticated;
        /// <summary>
        ///* This event is triggered when a new player is connected to the matchmaking.
        /// </summary>
        public event NeutronEventNoReturn<NeutronPlayer, bool, Neutron> OnPlayerConnected;
        /// <summary>
        ///* This event is triggered when a player is disconnected from the matchmaking.
        /// </summary>
        public event NeutronEventNoReturn<string, NeutronPlayer, bool, Neutron> OnPlayerDisconnected;
        /// <summary>
        ///* This event is triggered when a player sends a message.
        /// </summary>
        public event NeutronEventNoReturn<string, NeutronPlayer, bool, Neutron> OnMessageReceived;
        /// <summary>
        ///* This event is triggered when a channel list is received.
        /// </summary>
        public event NeutronEventNoReturn<NeutronChannel[], Neutron> OnChannelsReceived;
        /// <summary>
        ///* This event is triggered when a room list is received.
        /// </summary>
        public event NeutronEventNoReturn<NeutronRoom[], Neutron> OnRoomsReceived;
        /// <summary>
        ///* This event is triggered when a player left channel.
        /// </summary>
        public event NeutronEventNoReturn<NeutronChannel, NeutronPlayer, bool, Neutron> OnPlayerLeftChannel;
        /// <summary>
        ///* This event is triggered when a player left room.
        /// </summary>
        public event NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Neutron> OnPlayerLeftRoom;
        /// <summary>
        ///* This event is triggered when a new room is created.
        /// </summary>
        public event NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Neutron> OnPlayerCreatedRoom;
        /// <summary>
        ///* This event is triggered when a player joined a room.
        /// </summary>
        public event NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Neutron> OnPlayerJoinedRoom;
        /// <summary>
        ///* This event is triggered when a player joined a channel.
        /// </summary>
        public event NeutronEventNoReturn<NeutronChannel, NeutronPlayer, bool, Neutron> OnPlayerJoinedChannel;
        /// <summary>
        ///* This event is triggered when a player changes its nickname.
        /// </summary>
        public event NeutronEventNoReturn<NeutronPlayer, string, bool, Neutron> OnPlayerNicknameChanged;
        /// <summary>
        ///* This event is triggered when a player changes its status.
        /// </summary>
        public event NeutronEventNoReturn<NeutronPlayer, string, bool, Neutron> OnPlayerPropertiesChanged;
        /// <summary>
        ///* This event is triggered when a player changes room status.
        /// </summary>
        public event NeutronEventNoReturn<NeutronPlayer, string, bool, Neutron> OnRoomPropertiesChanged;
        /// <summary>
        ///* This event is triggered when a player sends a custom packet.
        /// </summary>
        public event NeutronEventNoReturn<NeutronStream.IReader, NeutronPlayer, byte, Neutron> OnPlayerCustomPacketReceived;
        /// <summary>
        ///* This event is triggered when your get an error.
        /// </summary>
        public event NeutronEventNoReturn<Packet, string, int, Neutron> OnError;
        #endregion

        #region Yields
        /// <summary>
        ///* Timer of udp keep alive.
        /// </summary>
        private WaitForSeconds _yieldUdpKeepAlive;
        /// <summary>
        ///* Timer of tcp keep alive.
        /// </summary>
        private WaitForSeconds _yieldTcpKeepAlive;
        #endregion

        #region Methods -> Instance
#pragma warning disable IDE0051
        private void Update()
#pragma warning restore IDE0051 
        {
            if (NeutronModule.IsUnityThread)
                PacketProcessingStack();
        }
        /// <summary>
        ///* Connects to the server.
        /// </summary>
        /// <param name="index">index of the address</param>
        /// <param name="timeout">timeout of the connection</param>
        /// <param name="authentication">authentication of the connection</param>
        /// <returns></returns>
        public async void Connect(int index = 0, int timeout = 3, Authentication authentication = null)
        {
#if UNITY_EDITOR
            ThreadManager.WarnSimultaneousAccess(); //* Warns if there is a simultaneous access.
#endif
#if UNITY_SERVER && !UNITY_EDITOR
            if (ClientMode == ClientMode.Player)
                return; //* If the client is a player, it can't connect.
#endif
            if (authentication == null)
                authentication = Authentication.Auth; //* If the authentication is null, it will be set to the default authentication.
            if (!IsConnected)
            {
                StartSocket(); //* Starts the socket.
                TcpClient.NoDelay = Settings.GlobalSettings.NoDelay; //* Sets the no delay.
                TcpClient.ReceiveBufferSize = Constants.Tcp.TcpReceiveBufferSize; //* Sets the receive buffer size.
                TcpClient.SendBufferSize = Constants.Tcp.TcpSendBufferSize; //* Sets the send buffer size.
                UdpClient.Client.ReceiveBufferSize = Constants.Udp.UdpReceiveBufferSize; //* Sets the receive buffer size.
                UdpClient.Client.SendBufferSize = Constants.Udp.UdpSendBufferSize; //* Sets the send buffer size.

                _yieldUdpKeepAlive = new WaitForSeconds(Settings.ClientSettings.UdpKeepAlive); //* Sets the udp keep alive.
                _yieldTcpKeepAlive = new WaitForSeconds(Settings.ClientSettings.TcpKeepAlive); //* Sets the tcp keep alive.

                //* Obtém o ip do URL setado nas configurações.
                #region Host Resolver
                int port = Settings.GlobalSettings.Port; //* Port of the server.
                _host = Settings.GlobalSettings.Addresses[index]; //* Host of the server.
                if (!string.IsNullOrEmpty(_host))
                {
                    //* If the host is not empty, it will be resolved.
                    if (!IPAddress.TryParse(_host, out IPAddress _))
                    {
                        //* If the host is not an ip, it will be resolved.
                        if (!_host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _host = _host.Replace("http://", string.Empty);
                            _host = _host.Replace("https://", string.Empty);
                            _host = _host.Replace("/", string.Empty);
                            _host = (await SocketHelper.GetHostAddress(_host)).ToString();
                        }
                        else
                            _host = IPAddress.Loopback.ToString(); //* If the host is localhost, it will be resolved.
                    }
                    else { /*Continue*/ } //* If the host is an ip, it will be resolved.
                }
                else
                    _host = IPAddress.Loopback.ToString(); //* If the host is empty, it will be resolved.
                #endregion

                bool result = await TcpClient.ConnectAsync(_host, port).RunWithTimeout(new TimeSpan(0, 0, timeout)); //* Connects to the server.
                if (!result)
                {
                    Internal_OnNeutronConnected(IsConnected, () =>
                    {
                        OnNeutronConnected?.Invoke(IsConnected, this); //* Triggers the OnNeutronConnected event.
                    }, this);

                    if (!LogHelper.Error("An attempt to establish a connection to the server failed."))
                        Dispose(); //* Disposes the client.
                }
                else if (result)
                {
                    IsConnected = TcpClient.Connected; //* Sets the connection status.
                    Internal_OnNeutronConnected(IsConnected, () =>
                    {
                        OnNeutronConnected?.Invoke(IsConnected, this); //* Triggers the OnNeutronConnected event.
                    }, this);

                    Stream networkStream = SocketHelper.GetStream(TcpClient); //* Gets the network stream.
                    if (!NeutronModule.IsUnityThread)
                    {
                        ThreadPool.QueueUserWorkItem((e) =>
                        {
                            OnReceivingDataLoop(networkStream, Protocol.Tcp);
                            OnReceivingDataLoop(networkStream, Protocol.Udp);
                        });
                    }
                    else
                    {
                        StartCoroutine(OnReceivingDataCoroutine(networkStream, Protocol.Tcp));
                        StartCoroutine(OnReceivingDataCoroutine(networkStream, Protocol.Udp));
                    }

                    if (!NeutronModule.IsUnityThread)
                    {
                        Thread packetProcessingStackTh = new Thread((e) =>
                        {
                            while (!TokenSource.Token.IsCancellationRequested)
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

                    NeutronSchedule.ScheduleTask(UdpKeepAlive()); //* Schedules the udp keep alive.
                    NeutronSchedule.ScheduleTask(TcpKeepAlive()); //* Schedules the tcp keep alive.
                    HandAuth();
                }
            }
            else
                HandAuth();

            void HandAuth()
            {
                try
                {
                    if (!IsReady)
                    {
                        //* If the client is not ready, it will be authenticated.
                        using (NeutronStream stream = PooledNetworkStreams.Pull())
                        {
                            NeutronStream.IWriter writer = stream.Writer;
                            string appId = Settings.GlobalSettings.AppId;
                            writer.WritePacket((byte)Packet.Handshake);
                            writer.Write(appId.Encrypt());
                            writer.Write(NetworkTime.LocalTime);
                            writer.WriteWithInteger(authentication);
                            Send(stream); //* Sends the packet.
                        }
                    }
                    else
                        LogHelper.Error("It is no longer possible to initialize a connection on this instance.");
                }
                catch
                {
                    LogHelper.Error("It is no longer possible to initialize a connection on this instance.");
                }
            }
        }

        /// <summary>
        ///* Create the udp packet with received bytes.
        /// </summary>
        private void CreateUdpPacket()
        {
            byte[] datagram = StateObject.SlicedDatagram;
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                //* If the received datagram is not empty, it will be created.
                NeutronStream.IReader reader = stream.Reader;
                reader.SetBuffer(datagram); //* Sets the buffer.

                int playerId = reader.ReadShort(); //* Reads the player id.
                byte[] pBuffer = reader.ReadNext(); //* Reads the packet buffer.
                pBuffer = pBuffer.Decompress(); //* Decompresses the packet buffer.

                NeutronPlayer player = Players[playerId]; //* Gets the player.
                NeutronPacket neutronPacket = Helper.PollPacket(pBuffer, player, player, Protocol.Udp); //* Creates the neutron packet.

                _dataForProcessing.Push(neutronPacket); //* Adds the packet to the processing queue.
                NeutronStatistics.ClientUDP.AddIncoming(datagram.Length); //* Adds the received bytes to the statistics.
            }
        }

        /// <summary>
        ///* Read the udp data.
        /// </summary>
        private void UdpApmReceive()
        {
            if (TokenSource.Token.IsCancellationRequested)
                return; //* If the token is cancelled, it will return.

            SocketHelper.BeginReadBytes(UdpClient, StateObject, UdpApmEndReceive); //* Starts the udp receive.
        }

        /// <summary>
        ///* Read the udp data and create the udp packet.
        /// </summary>
        /// <param name="ar"></param>
        private void UdpApmEndReceive(IAsyncResult ar)
        {
            EndPoint remoteEp = StateObject.NonAllocEndPoint; //* Gets the remote endpoint.
            int bytesRead = SocketHelper.EndReadBytes(UdpClient, ref remoteEp, ar); //* Reads the udp data.
            if (bytesRead > 0)
            {
                //* If the bytes are read, it will be created.
                StateObject.SlicedDatagram = new byte[bytesRead]; //* Sets the received datagram, avoid GC Alloc in the future.
                Buffer.BlockCopy(StateObject.ReceivedDatagram, 0, StateObject.SlicedDatagram, 0, bytesRead); //* Copies the received bytes.
                CreateUdpPacket(); //* Creates the udp packet.
            }

            if (!NeutronModule.IsUnityThread)
                UdpApmReceive(); //* Starts the udp receive again.
        }

        /// <summary>
        ///* Starts the packet processing stack.
        /// </summary>
        private void PacketProcessingStack()
        {
            //* If the token is not cancelled, it will process the packets.
            NeutronPacket packet = _dataForProcessing.Pull(); //* Gets the packet and blocks the thread.
            if (packet != null)
            {
                RunPacket(packet.Owner, packet.Buffer); //* Runs the packet.
                packet.Recycle(); //* Recycles the packet.
            }
        }

        private IEnumerator OnReceivingDataCoroutine(Stream networkStream, Protocol protocol)
        {
            CancellationToken token = TokenSource.Token; //* Gets the token.
            byte[] hBuffer = new byte[NeutronModule.HeaderSize]; //* Sets the header buffer, store de length of the packet..etc.
            byte[] pIBuffer = new byte[sizeof(short)]; //* Sets the owner id of the packet.
            while (!token.IsCancellationRequested)
            {
                switch (protocol)
                {
                    case Protocol.Tcp:
                        {
                            var headerTask = SocketHelper.ReadAsyncBytes(networkStream, hBuffer, 0, NeutronModule.HeaderSize, token).AsCoroutine();
                            yield return headerTask;
                            if (headerTask.Result)
                            {
                                //* If the header is read, it will be created.
                                int size = ByteHelper.ReadSize(hBuffer); //* Gets the size of the packet and read it.
                                if (size <= Constants.Tcp.MaxTcpPacketSize)
                                {
                                    //* If the size is less than the max tcp packet size, it will be created.
                                    byte[] pBuffer = new byte[size]; //* create the buffer with the size of the packet.
                                    var pBufferTask = SocketHelper.ReadAsyncBytes(networkStream, pBuffer, 0, size, token).AsCoroutine();
                                    yield return pBufferTask;
                                    if (pBufferTask.Result)
                                    {
                                        //* If the packet is read, it will be created.
                                        pBuffer = pBuffer.Decompress(); //* Decompresses the packet.
                                        var pIBufferTask = SocketHelper.ReadAsyncBytes(networkStream, pIBuffer, 0, sizeof(short), token).AsCoroutine();
                                        yield return pIBufferTask;
                                        if (pIBufferTask.Result)
                                        {
                                            //* If the owner id is read, it will be created.
                                            int playerId = BitConverter.ToInt16(pIBuffer, 0); //* Gets the owner id.
                                            if (playerId <= Settings.GlobalSettings.MaxPlayers && playerId >= 0)
                                            {
                                                //* If the owner id is valid, it will be created.
                                                NeutronPlayer player = Players[playerId]; //* Gets the player.
                                                NeutronPacket neutronPacket = Helper.PollPacket(pBuffer, player, player, Protocol.Tcp);

                                                _dataForProcessing.Push(neutronPacket); //* Adds the packet to the processing queue.
                                                NeutronStatistics.ClientTCP.AddIncoming(size + hBuffer.Length + pIBuffer.Length); //* Adds the received bytes to the statistics.
                                            }
                                            else
                                                LogHelper.Error($"Player({playerId}) not found!!!!");
                                        }
                                        else
                                            Dispose(); //* Disconnects the client.
                                    }
                                    else
                                        Dispose(); //* Disconnects the client.
                                }
                                else
                                    LogHelper.Error($"Packet size exceeds defined limit!! size: {size}");
                            }
                            else
                                Dispose(); //* Disconnects the client.
                        }
                        break;
                    case Protocol.Udp:
                        {
                            switch (Constants.ReceiveAsyncPattern)
                            {
                                //* If the receive async pattern is set to Asynchronous APM Mode, it will be used.
                                case AsynchronousType.TAP:
                                    {
                                        var datagramTask = SocketHelper.ReadAsyncBytes(UdpClient, StateObject);
                                        yield return datagramTask;
                                        if (datagramTask.Result)
                                            CreateUdpPacket(); //* Creates the udp packet.
                                    }
                                    break;
                                default:
                                    UdpApmReceive(); //* Starts the udp receive.
                                    break;
                            }
                        }
                        break;
                }
                yield return null;
            }
        }

        /// <summary>
        ///* Read the tcp or udp data.
        /// </summary>
        private async void OnReceivingDataLoop(Stream networkStream, Protocol protocol)
        {
            CancellationToken token = TokenSource.Token; //* Gets the token.
            try
            {
                bool whileOn = false; //* used to stop the while loop.
                byte[] hBuffer = new byte[NeutronModule.HeaderSize]; //* Sets the header buffer, store de length of the packet..etc.
                byte[] pIBuffer = new byte[sizeof(short)]; //* Sets the owner id of the packet.
                while (!token.IsCancellationRequested && !whileOn)
                {
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                if (await SocketHelper.ReadAsyncBytes(networkStream, hBuffer, 0, NeutronModule.HeaderSize, token))
                                {
                                    //* If the header is read, it will be created.
                                    int size = ByteHelper.ReadSize(hBuffer); //* Gets the size of the packet and read it.
                                    if (size <= Constants.Tcp.MaxTcpPacketSize)
                                    {
                                        //* If the size is less than the max tcp packet size, it will be created.
                                        byte[] pBuffer = new byte[size]; //* create the buffer with the size of the packet.
                                        if (await SocketHelper.ReadAsyncBytes(networkStream, pBuffer, 0, size, token))
                                        {
                                            //* If the packet is read, it will be created.
                                            pBuffer = pBuffer.Decompress(); //* Decompresses the packet.
                                            if (await SocketHelper.ReadAsyncBytes(networkStream, pIBuffer, 0, sizeof(short), token))
                                            {
                                                //* If the owner id is read, it will be created.
                                                int playerId = BitConverter.ToInt16(pIBuffer, 0); //* Gets the owner id.
                                                if (playerId <= Settings.GlobalSettings.MaxPlayers && playerId >= 0)
                                                {
                                                    //* If the owner id is valid, it will be created.
                                                    NeutronPlayer player = Players[playerId]; //* Gets the player.
                                                    NeutronPacket neutronPacket = Helper.PollPacket(pBuffer, player, player, Protocol.Tcp);

                                                    _dataForProcessing.Push(neutronPacket); //* Adds the packet to the processing queue.
                                                    NeutronStatistics.ClientTCP.AddIncoming(size + hBuffer.Length + pIBuffer.Length); //* Adds the received bytes to the statistics.
                                                }
                                                else
                                                    LogHelper.Error($"Player({playerId}) not found!!!!");
                                            }
                                            else
                                                Dispose(); //* Disconnects the client.
                                        }
                                        else
                                            Dispose(); //* Disconnects the client.
                                    }
                                    else
                                        LogHelper.Error($"Packet size exceeds defined limit!! size: {size}");
                                }
                                else
                                    Dispose(); //* Disconnects the client.
                            }
                            break;
                        case Protocol.Udp:
                            {
                                switch (Constants.ReceiveAsyncPattern)
                                {
                                    //* If the receive async pattern is set to Asynchronous APM Mode, it will be used.
                                    case AsynchronousType.TAP:
                                        if (await SocketHelper.ReadAsyncBytes(UdpClient, StateObject))
                                            CreateUdpPacket(); //* Creates the udp packet.
                                        break;
                                    default:
                                        UdpApmReceive(); //* Starts the udp receive.
                                        whileOn = true; //* Stops the while loop in apm mode.
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }

        /// <summary>
        ///* Run the packet of the queue.
        /// </summary>
        private void RunPacket(NeutronPlayer player, byte[] buffer)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
                reader.SetBuffer(buffer); //* Sets the buffer.
                Packet packet = (Packet)reader.ReadPacket(); //* Reads the packet.
                bool isMine = (IsReady && IsMine(player)) || packet == Packet.Handshake; //* If the packet is a handshake packet or the client is ready and is the owner of the packet, it will be run.
                switch (packet)
                {
                    case Packet.UdpKeepAlive:
                        {
                            var serverTime = reader.ReadDouble(); //* Gets the server time.
                            var clientTime = reader.ReadDouble(); //* Gets the client time.
                            if (NetworkTime.Rpu < NetworkTime.Spu)
                                NetworkTime.Rpu++; //* packet loss calc.
                            NetworkTime.GetNetworkTime(clientTime, serverTime); //* Gets the network time.
                        }
                        break;
                    case Packet.Handshake:
                        {
                            var serverTime = reader.ReadDouble(); //* Gets the server time.
                            var clientTime = reader.ReadDouble(); //* Gets the client time.
                            var udpPort = reader.ReadInt(); //* Gets the udp port of local player.
                            var localPlayer = reader.ReadWithInteger<NeutronPlayer>(); //* Gets the local player from server.

                            #region Udp Poll
                            UdpEndPoint = new NonAllocEndPoint(IPAddress.Parse(_host), udpPort); //* Sets the udp end point.
                            using (NeutronStream ping = PooledNetworkStreams.Pull())
                            {
                                //* Creates the ping packet.
                                NeutronStream.IWriter writer = ping.Writer;
                                for (int i = 0; i < 15; i++)
                                {
                                    //* Writes the ping packet 15 times, prevent packet loss.
                                    writer.WritePacket((byte)Packet.UdpKeepAlive);
                                    writer.Write(NetworkTime.LocalTime);
                                    Send(ping, Protocol.Udp);
                                }
                            }
                            #endregion

                            NetworkTime.GetNetworkTime(clientTime, serverTime); //* Gets the network time.
                            LocalPlayer = Players[localPlayer.Id]; //* Sets the local player.
                            LocalPlayer.Apply(localPlayer); //* Applies the updated properties of local player.
                            Internal_OnPlayerConnected(LocalPlayer, isMine, () =>
                            {
                                OnPlayerConnected?.Invoke(LocalPlayer, isMine, this); //* Calls the OnPlayerConnected event.
                            }, this);
                        }
                        break;
                    case Packet.Disconnection:
                        {
                            var reason = reader.ReadString(); //* reason of disconnection of the player.
                            Internal_OnPlayerDisconnected(reason, player, isMine, () =>
                            {
                                OnPlayerDisconnected?.Invoke(reason, player, isMine, this);
                            }, this);

                            Internal_OnPlayerLeftRoom(LocalPlayer.Room, player, isMine, () =>
                            {
                                OnPlayerLeftRoom?.Invoke(LocalPlayer.Room, player, isMine, this);
                            }, this);

                            Internal_OnPlayerLeftChannel(LocalPlayer.Channel, player, isMine, () =>
                            {
                                OnPlayerLeftChannel?.Invoke(LocalPlayer.Channel, player, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.Chat:
                        {
                            var message = reader.ReadString(); //* Gets the message sended by the player.
                            Internal_OnMessageReceived(message, player, isMine, () =>
                            {
                                OnMessageReceived?.Invoke(message, player, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.iRPC:
                        {
                            var registerType = (RegisterMode)reader.ReadPacket(); //* Gets the register type of neutronview object.
                            var viewID = reader.ReadShort(); //* Gets the view id of the neutronview object.
                            var rpcId = reader.ReadByte(); //* Gets the rpc id of the invoked method
                            var instanceId = reader.ReadByte(); //* Gets the script instance id of the invoked method.
                            var parameters = reader.ReadNext(); //* Gets the parameters of the invoked method.
                            iRPCHandler(rpcId, viewID, instanceId, parameters, player, registerType); //* Invokes the rpc method.
                        }
                        break;
                    case Packet.gRPC:
                        {
                            var rpcId = reader.ReadByte(); //* Gets the rpc id of the invoked method.
                            var parameters = reader.ReadNext(); //* gets the parameters of the invoked method.
                            gRPCHandler(rpcId, player, parameters, IsServer, isMine); //* Invokes the rpc method.
                        }
                        break;
                    case Packet.GetChannels:
                        {
                            var channels = reader.ReadWithInteger<NeutronChannel[]>(); //* Gets the channels from server.
                            Internal_OnChannelsReceived(channels, () =>
                            {
                                OnChannelsReceived?.Invoke(channels, this);
                            }, this);
                        }
                        break;
                    case Packet.JoinChannel:
                        {
                            var channel = reader.ReadWithInteger<NeutronChannel>(); //* Gets the joined channel.
                            Internal_OnPlayerJoinedChannel(channel, player, isMine, () =>
                            {
                                OnPlayerJoinedChannel?.Invoke(channel, player, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.Leave:
                        {
                            var mode = (MatchmakingMode)reader.ReadPacket(); //* Gets the matchmaking mode.
                            if (mode == MatchmakingMode.Channel)
                            {
                                var channel = reader.ReadWithInteger<NeutronChannel>(); //* Gets the left channel.
                                Internal_OnPlayerLeftChannel(channel, player, isMine, () =>
                                {
                                    OnPlayerLeftChannel?.Invoke(channel, player, isMine, this);
                                }, this);
                            }
                            else if (mode == MatchmakingMode.Room)
                            {
                                var room = reader.ReadWithInteger<NeutronRoom>(); //* Gets the left room.
                                Internal_OnPlayerLeftRoom(room, player, isMine, () =>
                                {
                                    OnPlayerLeftRoom?.Invoke(room, player, isMine, this);
                                }, this);
                            }
                        }
                        break;
                    case Packet.CreateRoom:
                        {
                            var room = reader.ReadWithInteger<NeutronRoom>(); //* Gets the created room.
                            Internal_OnPlayerCreatedRoom(room, player, isMine, () =>
                            {
                                OnPlayerCreatedRoom?.Invoke(room, player, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.GetRooms:
                        {
                            var rooms = reader.ReadWithInteger<NeutronRoom[]>(); //* Gets the rooms from server.
                            Internal_OnRoomsReceived(rooms, () =>
                            {
                                OnRoomsReceived?.Invoke(rooms, this);
                            }, this);
                        }
                        break;
                    case Packet.JoinRoom:
                        {
                            var room = reader.ReadWithInteger<NeutronRoom>(); //* Gets the joined room.
                            Internal_OnPlayerJoinedRoom(room, player, isMine, () =>
                            {
                                OnPlayerJoinedRoom?.Invoke(room, player, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.Destroy:
                        {
                            #region Logic
                            #endregion
                        }
                        break;
                    case Packet.Nickname:
                        {
                            var nickname = reader.ReadString(); //* Gets the nickname of the player.
                            Internal_OnPlayerNicknameChanged(player, nickname, isMine, () =>
                            {
                                OnPlayerNicknameChanged?.Invoke(player, nickname, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.SetPlayerProperties:
                        {
                            var properties = reader.ReadString(); //* Gets the properties of the player.
                            Internal_OnPlayerPropertiesChanged(player, properties, isMine, () =>
                            {
                                OnPlayerPropertiesChanged?.Invoke(player, properties, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.SetRoomProperties:
                        {
                            var properties = reader.ReadString(); //* Gets the properties of the room.
                            Internal_OnRoomPropertiesChanged(player, properties, isMine, () =>
                            {
                                OnRoomPropertiesChanged?.Invoke(player, properties, isMine, this);
                            }, this);
                        }
                        break;
                    case Packet.CustomPacket:
                        {
                            var packetId = reader.ReadPacket(); //* Gets the packet id.
                            var parameters = reader.ReadWithInteger(); //* Gets the parameters of the packet.
                            using (NeutronStream cPStream = PooledNetworkStreams.Pull())
                            {
                                NeutronStream.IReader pReader = cPStream.Reader;
                                pReader.SetBuffer(parameters); //* Sets the parameters of the packet to read.
                                Internal_OnPlayerCustomPacketReceived(pReader, player, packetId, () =>
                                {
                                    OnPlayerCustomPacketReceived?.Invoke(pReader, player, packetId, this);
                                }, this);
                            }
                        }
                        break;
                    case Packet.AutoSync:
                        {
                            var registerType = (RegisterMode)reader.ReadPacket(); //* Gets the register type of the neutronview object.
                            var viewID = reader.ReadShort(); //* Gets the view id of the neutronview object.
                            var instanceId = reader.ReadByte(); //* Gets the script instance id of the invoked method.
                            var parameters = reader.ReadNext(); //* Gets the parameters of the invoked method.
                            AutoSyncHandler(player, viewID, instanceId, parameters, registerType);
                        }
                        break;
                    case Packet.AuthStatus:
                        {
                            var properties = reader.ReadString(); //* Gets the properties of the player.
                            var status = reader.ReadBool(); //* Gets the status of the authentication.
                            var parsedProperties = JObject.Parse(properties); //* Parses the properties to JsonObject.
                            Internal_OnNeutronAuthenticated(status, parsedProperties, () =>
                            {
                                OnNeutronAuthenticated?.Invoke(status, parsedProperties, this);
                            }, this);
                        }
                        break;
                    case Packet.Synchronize:
                        {
                            var mode = reader.ReadByte(); //* Gets the synchronization mode.
                            void SetState(NeutronPlayer[] otherPlayers)
                            {
                                //* Sets the state of the player.
                                foreach (var pPlayer in otherPlayers)
                                {
                                    if (pPlayer.Equals(This.LocalPlayer))
                                        continue; //* Skips the local player.

                                    var currentPlayer = Players[pPlayer.Id];
                                    currentPlayer.Apply(pPlayer); //* Applies the state of the player.

                                    if (!currentPlayer.IsConnected)
                                    {
                                        if (LocalPlayer.IsConnected)
                                        {
                                            Internal_OnPlayerConnected(currentPlayer, false, () =>
                                            {
                                                OnPlayerConnected?.Invoke(currentPlayer, false, this);
                                            }, this);
                                        }
                                    }

                                    if (!currentPlayer.IsInChannel() && !currentPlayer.IsInRoom())
                                    {
                                        if (LocalPlayer.IsInChannel())
                                        {
                                            var channel = LocalPlayer.Channel; //* Gets the channel of the local player.
                                            Internal_OnPlayerJoinedChannel(channel, currentPlayer, false, () =>
                                            {
                                                OnPlayerJoinedChannel?.Invoke(channel, currentPlayer, false, this);
                                            }, this);
                                        }
                                    }

                                    if (currentPlayer.IsInChannel() && !currentPlayer.IsInRoom())
                                    {
                                        if (LocalPlayer.IsInRoom())
                                        {
                                            var room = LocalPlayer.Room;
                                            Internal_OnPlayerJoinedRoom(room, currentPlayer, false, () =>
                                            {
                                                OnPlayerJoinedRoom?.Invoke(room, currentPlayer, false, this);
                                            }, this);
                                        }
                                    }
                                }
                            }

                            if (mode == 1)
                            {
                                var aBuffer = reader.ReadNext(); //* Gets the array of the players.
                                aBuffer = aBuffer.Decompress(CompressionMode.Deflate); //* Decompresses the array of the players.
                                var players = aBuffer.Deserialize<NeutronPlayer[]>(); //* Deserializes the array of the players.
                                SetState(players); //* Sets the state of the players.
                                synchronizeTcs.TrySetResult(true); //* Sets the result of the synchronization.
                                synchronizeTcs = new TaskCompletionSource<bool>(); //* Creates a new task completion source.
                            }
                            else if (mode == 2)
                            {
                                var pBuffer = reader.ReadNext(); //* Gets the player.
                                pBuffer = pBuffer.Decompress(CompressionMode.Deflate); //* Decompresses the player.
                                var localPlayer = pBuffer.Deserialize<NeutronPlayer>(); //* Deserializes the player.
                                SetState(new NeutronPlayer[] { localPlayer }); //* Sets the state of the player.
                            }
                        }
                        break;
                    case Packet.Error:
                        {
                            var capturedPacket = (Packet)reader.ReadPacket(); //* Gets the captured packet.
                            var message = reader.ReadString(); //* Gets the error message.
                            var errorCode = reader.ReadInt(); //* Gets the error code.
                            Internal_OnError(capturedPacket, message, errorCode, () =>
                            {
                                OnError?.Invoke(capturedPacket, message, errorCode, this);
                            }, this);
                        }
                        break;
                }
            }
        }
        #endregion

        #region Methods -> Instance -> Packets
        /// <summary>
        ///* Send the maintenence packet(Udp) to the server in X seconds.<br/>
        ///* This packet is used to maintain the connection with the server.<br/>
        ///* The player will be disconnected if the packet is not received in the specified time.<br/>
        /// </summary>
        /// <returns></returns>
        private IEnumerator UdpKeepAlive()
        {
            yield return new WaitUntil(() => IsReady);
            while (!TokenSource.Token.IsCancellationRequested)
            {
                NetworkTime.Spu++;
                using (NeutronStream stream = PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.UdpKeepAlive);
                    writer.Write(NetworkTime.LocalTime);
                    Send(stream, Protocol.Udp);
                }
                yield return _yieldUdpKeepAlive;
            }
        }

        /// <summary>
        ///* Send the maintenence packet(Tcp) to the server in X seconds.<br/>
        ///* This packet is used to maintain the connection with the server.<br/>
        ///* The player will be disconnected if the packet is not received in the specified time.<br/>
        /// </summary>
        /// <returns></returns>
        private IEnumerator TcpKeepAlive()
        {
            yield return new WaitUntil(() => IsReady); //* Waits until the player is ready.
            while (!TokenSource.Token.IsCancellationRequested)
            {
                using (NeutronStream stream = PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.TcpKeepAlive);
                    Send(stream, Protocol.Tcp);
                }
                yield return _yieldTcpKeepAlive; //* Waits until the next keep alive.
            }
        }

        /// <summary>
        ///* Leave from specified matchking.<br/>
        ///* The event <see cref="OnPlayerLeftRoom"/> or <see cref="OnPlayerLeftChannel"/> will be invoked when the player is disconnected from the matchking.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side).<br/>
        /// </summary>
        /// <param name="mode">The matchmaking mode.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void Leave(MatchmakingMode mode, NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Leave);
                writer.WritePacket((byte)mode);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Send chat message to the specified matchmaking.<br/>
        ///* The event <see cref="OnMessageReceived"/> will be invoked when the chat message is received.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side).<br/>
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="matchmakingTo">The matchmaking to send the message.</param>
        public void SendMessage(string message, MatchmakingTo matchmakingTo)
        {
#if !UNITY_SERVER || UNITY_EDITOR //* The server can't send messages.
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Chat);
                writer.WritePacket((byte)ChatMode.Global);
                writer.WritePacket((byte)matchmakingTo);
                writer.Write(message ?? string.Empty);
                Send(stream, Protocol.Tcp);
            }
#else
            LogHelper.Error("This packet is not available on the server side.");
#endif
        }

        /// <summary>
        ///* Send chat message to the specified player.<br/>
        ///* The event <see cref="OnMessageReceived"/> will be invoked when the chat message is received.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side).<br/>
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="player">The player to send the message.</param>
        public void SendMessage(string message, NeutronPlayer player)
        {
#if !UNITY_SERVER || UNITY_EDITOR //* The server can't send messages.
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Chat);
                writer.WritePacket((byte)ChatMode.Private);
                writer.Write(player.Id);
                writer.Write(message ?? string.Empty);
                Send(stream, Protocol.Tcp);
            }
#else
            LogHelper.Error("This packet is not available on the server side.");
#endif
        }

        /// <summary>
        ///* Send the custom packet to the specified matchmaking.<br/>
        ///* The event <see cref="OnPlayerCustomPacketReceived"/> will be invoked when the custom packet is received.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side).<br/>
        /// </summary>
        /// <param name="parameters">The parameters to send.</param>
        /// <param name="packet">The custom packet to send.</param>
        /// <param name="targetTo">These define wich remote will receive the packet.</param>
        /// <param name="matchmakingTo">The matchmaking to send the packet.</param>
        /// <param name="protocol">The protocol to send the packet.</param>
        public void SendCustomPacket(NeutronStream.IWriter parameters, byte packet, TargetTo targetTo, MatchmakingTo matchmakingTo, Protocol protocol)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.CustomPacket);
                writer.Write(LocalPlayer.Id);
                writer.WritePacket(packet);
                writer.WritePacket((byte)targetTo);
                writer.WritePacket((byte)matchmakingTo);
                writer.WriteWithInteger(parameters);
                Send(stream, protocol);
            }
        }

        /// <summary>
        ///* Send the auto-sync method to current matchmaking.<br/>
        ///* The virtual method "NeutronBehaviour.OnAutoSynchronization" will be invoked when the auto-sync packet is received.<br/>
        /// </summary>
        /// <param name="stream">The parameters to send.</param>
        /// <param name="view">The NeutronView object who is sending the packet.</param>
        /// <param name="instanceId">The script instance id of the NeutronBehaviour who is sending the packet.</param>
        /// <param name="protocol">The protocol to send the packet.</param>
        /// <param name="isServerSide">If true, the packet will be sent from server to client, otherwise from client to server.</param>
        [Network(PacketSize.AutoSync)] //* The default byte size is the size of the packet.
        public void OnAutoSynchronization(NeutronStream stream, NeutronView view, byte instanceId, Protocol protocol, bool isServerSide = false)
        {
            NeutronStream.IWriter writer = stream.Writer;
            if (writer.GetPosition() == 0)
            {
                writer.WritePacket((byte)Packet.AutoSync);
                writer.WritePacket((byte)view.RegisterMode);
                writer.Write((short)view.Id);
                writer.Write(instanceId);
                Send(stream, view.Owner, isServerSide, protocol);
            }
            else
                LogHelper.Error($"AutoSync: You are called writer.Write(); ?");
        }

        /// <summary>
        ///* Initiates a gRPC(Global Remote Procedure Call) service call.<br/>
        /// </summary>
        /// <param name="stream">The parameters to send.</param>
        [Network(PacketSize.gRPC)] //* The default byte size is the size of the packet.
        public NeutronStream.IWriter Begin_gRPC(NeutronStream stream)
        {
            NeutronStream.IWriter writer = stream.Writer;
            writer.SetPosition(PacketSize.gRPC); //* The parameters will be written after the specified size/position, before the specified size/position will be defined, will be written the header of the packet. 
            return writer;
        }

        /// <summary>
        ///* Send the gRPC(Global Remote Procedure Call) service call.<br/>
        ///* After this packet will be sent, the delegate method with the specified id will be invoked.<br/>
        /// </summary>
        /// <param name="id">The id of the gRPC service.</param>
        /// <param name="stream">The parameters of the gRPC service.</param>
        /// <param name="protocol">The protocol to send the packet.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
#pragma warning disable IDE1006
        [Network(PacketSize.gRPC)] //* The default byte size is the size of the packet.
        public void End_gRPC(byte id, NeutronStream stream, Protocol protocol, NeutronPlayer player = null)
#pragma warning restore IDE1006
        {
            NeutronStream.IWriter writer = stream.Writer;
            if (writer.GetPosition() == 0)
            {
                writer.WritePacket((byte)Packet.gRPC);
                writer.Write(id);
                Send(stream, player, protocol);
            }
            else
                LogHelper.Error($"gRPC: You are called writer.Write(); ?");
        }

        /// <summary>
        ///* Initiates a iRPC(Global Remote Procedure Call) service call.<br/>
        /// </summary>
        /// <param name="stream">The parameters to send.</param>
        /// <returns></returns>
        [Network(PacketSize.iRPC)] //* The default byte size is the size of the packet.
        public NeutronStream.IWriter Begin_iRPC(NeutronStream stream)
        {
            NeutronStream.IWriter writer = stream.Writer;
            writer.SetPosition(PacketSize.iRPC);
            return writer;
        }

        /// <summary>
        ///* Send the iRPC(Global Remote Procedure Call) service call.<br/>
        ///* After this packet will be sent, the delegate method with the specified id will be invoked.<br/>
        /// </summary>
        /// <param name="stream">The parameters of the iRPC service.</param>
        /// <param name="view">The NeutronView object who is sending the packet.</param>
        /// <param name="rpcId">The id of the iRPC service.</param>
        /// <param name="instanceId">The script instance id of the NeutronBehaviour who is sending the packet.</param>
        /// <param name="cache">Defines how the service will be cached on the server side.</param>
        /// <param name="targetTo">These define wich remote will receive the packet.</param>
        /// <param name="protocol">The protocol to send the packet.</param>
        /// <param name="isServerSide">If true, the packet will be sent from server to client, otherwise from client to server.</param>
        [Network(PacketSize.iRPC)] //* The default byte size is the size of the packet.
        public void End_iRPC(NeutronStream stream, NeutronView view, byte rpcId, byte instanceId, CacheMode cache, TargetTo targetTo, Protocol protocol, bool isServerSide = false)
        {
            NeutronStream.IWriter writer = stream.Writer;
            if (writer.GetPosition() == 0)
            {
                writer.WritePacket((byte)Packet.iRPC);
                writer.WritePacket((byte)view.RegisterMode);
                writer.WritePacket((byte)targetTo);
                writer.WritePacket((byte)cache);
                writer.Write((short)view.Id);
                writer.Write(rpcId);
                writer.Write(instanceId);
                Send(stream, view.Owner, isServerSide, protocol);
            }
            else
                LogHelper.Error($"iRPC: You are called writer.Write(); ?");
        }

        /// <summary>
        ///* Sets the nickname of the local player.<br/>
        ///* The event <see cref="OnPlayerNicknameChanged"/> will be invoked when the nickname is changed.<br/>
        ///* The event <see cref="OnError"/> will be invoked when the nickname is invalid.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="nickname">The nickname to set.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void SetNickname(string nickname, NeutronPlayer player = null)
        {
            if (!IsServer)
                Nickname = nickname; //* If is server side, the nickname of local instance not will be changed.
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Nickname);
                writer.Write(nickname);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Join to the specified channel.<br/>
        ///* The event <see cref="OnPlayerJoinedChannel"/> will be invoked when a player is joined.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="channelId">The id of the channel to join.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void JoinChannel(int channelId, NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.JoinChannel);
                writer.Write(channelId);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Join to the specified room.<br/>
        ///* The event <see cref="OnPlayerJoinedRoom"/> will be invoked when a player is joined.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="roomId">The id of the room to join.</param>
        /// <param name="password">The password of the room to join.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void JoinRoom(int roomId, string password = "", NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.JoinRoom);
                writer.Write(roomId);
                writer.Write(password ?? string.Empty);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Create a new room.<br/>
        ///* The event <see cref="OnPlayerCreatedRoom"/> will be invoked when a room is created.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="room">The room to create.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void CreateRoom(NeutronRoom room, NeutronPlayer player = null)
        {
            room.Owner = LocalPlayer;
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.CreateRoom);
                writer.Write(room.Password);
                writer.WriteWithInteger(room);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Get cache saved on the server side of the specified method.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="packet">The packet to get the cache.</param>
        /// <param name="Id">The id of the method to get the cache.</param>
        /// <param name="includeOwnerPackets">If true, the owner packets will be included in the cache.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void GetCache(CachedPacket packet = CachedPacket.All, byte Id = 0, bool includeOwnerPackets = true, NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.GetCache);
                writer.WritePacket((byte)packet);
                writer.Write(Id);
                writer.Write(includeOwnerPackets);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Get the list of channels.<br/>
        ///* The event <see cref="OnChannelsReceived"/> will be invoked when the list is received.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void GetChannels(NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.GetChannels);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Get the list of rooms.<br/>
        ///* The event <see cref="OnRoomsReceived"/> will be invoked when the list is received.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void GetRooms(NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.GetRooms);
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Set the player properties.<br/>
        ///* The event <see cref="OnPlayerPropertiesChanged"/> will be invoked when the properties are changed.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="properties">The properties to set.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void SetPlayerProperties(Dictionary<string, object> properties, NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.SetPlayerProperties);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(stream, player, Protocol.Tcp);
            }
        }

        /// <summary>
        ///* Set the room properties.<br/>
        ///* The event <see cref="OnRoomPropertiesChanged"/> will be invoked when the properties are changed.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="properties">The properties to set.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void SetRoomProperties(Dictionary<string, object> properties, NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.SetRoomProperties);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(stream, player, Protocol.Tcp);
            }
        }

        private TaskCompletionSource<bool> synchronizeTcs = new TaskCompletionSource<bool>(); //* Synchronize task completion source.
        /// <summary>
        ///* Synchronize all clients with each other.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        /// <returns></returns>
        public Task Synchronize(NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Synchronize);
                Send(stream, player, Protocol.Tcp);
            }
            return synchronizeTcs.Task;
        }

        /// <summary>
        ///* Send raw bytes to current matchmaking.<br/>
        ///* The event <see cref="OnError"/> will be invoke if error occurs.<br/>
        ///* (Client-Side) or (Server-Side)<br/>
        /// </summary>
        /// <param name="buffer">The buffer to send.</param>
        /// <param name="protocol">The protocol to send.</param>
        /// <param name="player">Set to send from server to specified client, if null will be sent from client to server.</param>
        public void SendRawBytes(byte[] buffer, Protocol protocol, NeutronPlayer player = null)
        {
            using (NeutronStream stream = PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.Write(buffer);
                Send(stream, player, protocol);
            }
        }
        #endregion

        #region Methods -> Static
        /// <summary>
        ///* Create a new instance of <see cref="Neutron"/>.<br/>
        ///* The instances are independent of each other, each instance is a new connection/client.<br/>
        /// </summary>
        /// <param name="clientMode">The mode of client.</param>
        /// <returns></returns>
        public static Neutron Create(ClientMode clientMode = ClientMode.Player)
        {
            Neutron neutron = NeutronModule.ClientObject.AddComponent<Neutron>(); //* Create new instance.
            #region Initialize Collections
            neutron._dataForProcessing = !NeutronModule.IsUnityThread ? (INeutronConsumer<NeutronPacket>)new NeutronBlockingQueue<NeutronPacket>() : (INeutronConsumer<NeutronPacket>)new NeutronSafeQueueNonAlloc<NeutronPacket>();
            #endregion
            neutron.Initialize(clientMode); //* Initialize instance and register events.
#if UNITY_SERVER && !UNITY_EDITOR //* If server and not editor.
            if (clientMode == ClientMode.Player)
            {
                LogHelper.Info($"The main player has been removed from the server build, but you can choose to use a virtual player!\r\n");
                return neutron;
            }
#endif
            if (neutron.Scene == default && Server != null)
                neutron.Scene = SceneHelper.CreateContainer(neutron._sceneName, physics: Server.LocalPhysicsMode); //* Create the default container to hold all the objects of client.
            if (clientMode == ClientMode.Player)
            {
                if (Client == null)
                    Client = neutron;
                else
                    LogHelper.Error("The main player has already been initialized, you don't want to create a virtual client?");
            }
            return neutron;
        }

        /// <summary>
        ///* Spawn a prefab in network at the specified position and rotation.<br/>
        /// </summary>
        /// <param name="isServer">Check if the current instance is server or not.</param>
        /// <param name="immediateSpawnOnLocalNetwork">If true, the prefab will be spawned immediately on local network.</param>
        /// <param name="player">The player to spawn the prefab.</param>
        /// <param name="prefab">The prefab to spawn.</param>
        /// <param name="position">The position to spawn the prefab.</param>
        /// <param name="rotation">The rotation to spawn the prefab.</param>
        /// <param name="neutron">The instance to spawn the prefab.</param>
        /// <returns></returns>
        public static GameObject NetworkSpawn(bool isServer, bool immediateSpawnOnLocalNetwork, NeutronPlayer player, GameObject prefab, Vector3 position, Quaternion rotation, Neutron neutron)
        {
            if (prefab.TryGetComponent(out NeutronView neutronView))
            {
                GameObject Spawn()
                {
                    GameObject gameObject = MonoBehaviour.Instantiate(prefab, position, rotation);
                    neutronView = gameObject.GetComponent<NeutronView>();
                    neutronView.OnNeutronRegister(player, isServer, neutron); //* Register the neutron view in the network.
                    return gameObject;
                }

                if (immediateSpawnOnLocalNetwork && neutron.IsMine(player))
                    return null;
                switch (neutronView.Side)
                {
                    case Side.Both:
                        {
                            return Spawn(); //* Spawn on both server and client.
                        }
                    case Side.Server:
                        {
                            if (isServer)
                                return Spawn(); //* Spawn on server.
                            else
                                break; //* Don't spawn on client.
                        }
                    case Side.Client:
                        {
                            if (!isServer)
                                return Spawn(); //* Spawn on client.
                            else
                                break; //* Don't spawn on server.
                        }
                }
            }
            else
                LogHelper.Error("Add the \"Neutron View\" component to instantiate a networked object.");
            return null;
        }
        #endregion
    }
}
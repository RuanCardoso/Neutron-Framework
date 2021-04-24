using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server.Delegates;
using NeutronNetwork.Internal.Wrappers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Created by: Ruan Cardoso
/// Email: cardoso.ruan050322@gmail.com
/// License: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>

namespace NeutronNetwork.Internal.Server
{
    [RequireComponent(typeof(NeutronConfig))]
    [RequireComponent(typeof(NeutronDispatcher))]
    [RequireComponent(typeof(NeutronEvents))]
    [RequireComponent(typeof(NeutronStatistics))]
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_SERVER)]
    public class NeutronServer : NeutronServerPublicFunctions
    {
        #region Events
        //* notifies you if the server has started.
        public static event Events.OnServerStart m_OnServerStart;
        public static event Events.OnServerStart m_OnPlayerConnected;
        #endregion

        #region Variables
        //* Signals that the server has been started.
        public static bool Initialized;
        //* all accepted clients will be queued here.
        #endregion

        #region Collections
        //* here the data received from clients for processing will be queued.
        //* all this processing is done in a single thread on the server, making this whole operation safe for threads.
        private NeutronQueue<TcpClient> acceptedClients = new NeutronQueue<TcpClient>();
        //* [Three Unique Thread] - do not use ThreadPool here.(These methods must have their own dedicated thread for processing.)
        //* Thread pool will join them with other threads that are already processing other methods. causing loss of performance, these must be unique.
        private NeutronQueue<DataBuffer> dataForProcessing = new NeutronQueue<DataBuffer>();
        //* provides a unique id for new client.
        public NeutronQueue<int> generatedIds = new NeutronQueue<int>();
        #endregion

        #region Threading
        CancellationTokenSource cts = new CancellationTokenSource();
        #endregion

        #region Functions
        private void InitilizeServer()
        {
            #region Provider
            for (int i = 0; i < NeutronConfig.Settings.GlobalSettings.MaxPlayers; i++)
                generatedIds.SafeEnqueue((Neutron.GENERATE_PLAYER_ID + i) + 1);
            #endregion

            Initialized = true;

            #region Logger
            NeutronUtils.Logger("The Server is ready, all protocols have been initialized.\r\n");
            #endregion

            #region Threads
            Thread acptTh = new Thread((o) => OnAcceptedClient()); //* exclusive thread to accept connections.
            acptTh.Priority = System.Threading.ThreadPriority.Normal;
            acptTh.IsBackground = true;
            acptTh.Start();
            //* This thread processes data received from clients.
            Thread dataForProcessingTh = new Thread((e) => ServerDataProcessingStack());
            dataForProcessingTh.Priority = System.Threading.ThreadPriority.Highest; //* set the max priority.
            dataForProcessingTh.IsBackground = true;
            dataForProcessingTh.Start();
            //* This thread processes the clients in the queue "acceptedClients". 
            Thread stackProcessingAcceptedConnectionsTh = new Thread((e) => AcceptedConnectionsProcessingStack()); // Thread dedicated to processing accepted clients.
            stackProcessingAcceptedConnectionsTh.Priority = System.Threading.ThreadPriority.Normal;
            stackProcessingAcceptedConnectionsTh.IsBackground = true;
            stackProcessingAcceptedConnectionsTh.Start();
            #endregion

            #region Events
            m_OnServerStart?.Invoke();
            #endregion
        }
        //* initiates client acceptance.
        private async void OnAcceptedClient()
        {
            try
            {
                CancellationToken token = cts.Token;
                while (Initialized && !token.IsCancellationRequested)
                {
                    if (token.IsCancellationRequested) return;
                    TcpClient tcpClient = await TcpSocket.AcceptTcpClientAsync();
                    acceptedClients.SafeEnqueue(tcpClient); //* [Thread-Safe]. adds the client to the queue.
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
        }
        //* start server data processing.
        private void ServerDataProcessingStack()
        {
            CancellationToken token = cts.Token;
            while (Initialized && !token.IsCancellationRequested) //* infinite loop to keep data processing active.
            {
                if (token.IsCancellationRequested) return;
                dataForProcessing.mEvent.Reset(); //* Sets the state of the event to nonsignaled, which causes threads to block.
                while (dataForProcessing.SafeCount > 0) //* thread-safe - loop to process all data in the queue, before blocking the thread.
                {
                    if (token.IsCancellationRequested) return;
                    for (int i = 0; i < NeutronConfig.Settings.ServerSettings.PacketChunkSize && dataForProcessing.SafeCount > 0; i++)
                    {
                        var data = dataForProcessing.SafeDequeue();
                        PacketProcessing(data.player, data.buffer, data.protocol);
                    }
                }
                dataForProcessing.mEvent.WaitOne(); //* Blocks the current thread until the current WaitHandle receives a signal.
            }
        }
        //* processes the queue clients. [Multiples Thread - ThreadPool for best perfomance.]
        private void AcceptedConnectionsProcessingStack()
        {
            CancellationToken token = cts.Token;
            while (Initialized && !token.IsCancellationRequested)
            {
                if (token.IsCancellationRequested) return;
                acceptedClients.mEvent.Reset(); //* Sets the state of the event to nonsignaled, which causes threads to block.
                while (acceptedClients.SafeCount > 0)
                {
                    if (token.IsCancellationRequested) return;
                    var acceptedClient = acceptedClients.SafeDequeue();
                    if (PlayerHelper.GetAvailableID(out int ID))
                    {
                        if (SocketHelper.LimitConnectionsByIP(acceptedClient))
                        {
                            acceptedClient.NoDelay = NeutronConfig.Settings.GlobalSettings.NoDelay;
                            // TODO acceptedClient.ReceiveTimeout = int.MaxValue;
                            // TODO acceptedClient.SendTimeout = int.MaxValue;
                            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(); //* Propagates notification that operations should be canceled.
                            Player newPlayer = new Player(ID, acceptedClient, cancellationTokenSource);
                            if (SocketHelper.AddPlayer(newPlayer))
                            {
                                CurrentPlayers++;
                                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if UNITY_SERVER
                        NeutronUtils.Logger($"Incoming client, IP: [{newPlayer.RemoteEndPoint().Address}] | TCP: [{newPlayer.RemoteEndPoint().Port}] | UDP: [{((IPEndPoint)newPlayer.udpClient.Client.LocalEndPoint).Port}] -:[{totalAmountOfPlayers}]");
#endif
                                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                Thread procTh = new Thread(() => OnProcessData(newPlayer, cancellationTokenSource.Token)); //* because this method locks the thread, if it falls on a segment of "OnReceive" the data will not be received.
                                procTh.IsBackground = true;
                                procTh.Start();
                                ThreadPool.QueueUserWorkItem((e) =>
                                {
                                    OnReceiveData(newPlayer, Protocol.Tcp, e);
                                    OnReceiveData(newPlayer, Protocol.Udp, e);
                                }, cancellationTokenSource.Token); //! Thread dedicated to receive data.
                            }
                        }
                        else continue;
                    }
                    else
                    {
                        #region Logger
                        NeutronUtils.LoggerError("Max Players Reached");
                        #endregion
                        acceptedClient.Close();
                        continue;
                    }
                }
                acceptedClients.mEvent.WaitOne(); //* Blocks the current thread until the current WaitHandle receives a signal.
            }
        }

        private void OnProcessData(Player player, object toToken)
        {
            try
            {
                CancellationToken token = (CancellationToken)toToken;
                var queueData = player.qData;
                ManualResetEvent manualResetEvent = queueData.mEvent;
                var netStream = player.tcpClient.GetStream();

                while (Initialized && !token.IsCancellationRequested)
                {
                    if (token.IsCancellationRequested) return;
                    manualResetEvent.Reset();
                    while (queueData.SafeCount > 0)
                    {
                        if (token.IsCancellationRequested) return;
                        for (int i = 0; i < NeutronConfig.Settings.ServerSettings.ProcessChunkSize && queueData.SafeCount > 0; i++)
                        {
                            var data = queueData.SafeDequeue();
                            using (NeutronWriter header = new NeutronWriter())
                            {
                                int dataLength = data.buffer.Length;
                                header.WriteFixedLength(dataLength); //* write length of message(header).
                                header.Write(data.buffer);
                                byte[] nBuffer = header.ToArray();
                                switch (data.protocol)
                                {
                                    case Protocol.Tcp:
                                        dataLength = nBuffer.Length;
                                        if (player.tcpClient != null)
                                            netStream.Write(nBuffer, 0, dataLength);
                                        break;
                                    case Protocol.Udp:
                                        if (player.rPEndPoint != null && player.tcpClient != null)
                                            player.udpClient.Send(data.buffer, dataLength, player.rPEndPoint);
                                        break;
                                }
                                InternalUtils.UpdateStatistics(Statistics.ServerSent, dataLength);
                            }
                        }
                    }
                    manualResetEvent.WaitOne();
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
        }

        private async void OnReceiveData(Player player, Protocol protocol, object toToken)
        {
            CancellationToken token = (CancellationToken)toToken;

            byte[] header = new byte[sizeof(int)];
            byte[] message;
            try
            {
                var netStream = player.tcpClient.GetStream();
                while (Initialized && !token.IsCancellationRequested)
                {
                    if (token.IsCancellationRequested) return;
                    if (protocol == Protocol.Tcp)
                    {
                        if (await Communication.ReadAsyncBytes(netStream, header, 0, sizeof(int), token))
                        {
                            int size = BitConverter.ToInt32(header, 0);
                            if (size > MAX_RECEIVE_MESSAGE_SIZE || size <= 0) DisconnectHandler(player);
                            else
                            {
                                message = new byte[size];
                                if (await Communication.ReadAsyncBytes(netStream, message, 0, size, token))
                                {
                                    dataForProcessing.SafeEnqueue(new DataBuffer(message, player, Protocol.Tcp));
                                    InternalUtils.UpdateStatistics(Statistics.ServerRec, size);
                                }
                                else DisconnectHandler(player);
                            }
                        }
                        else DisconnectHandler(player);
                    }
                    else if (protocol == Protocol.Udp)
                    {
                        var udpReceiveResult = await player.udpClient.ReceiveAsync();
                        if (udpReceiveResult.Buffer.Length > 0)
                        {
                            if (player.rPEndPoint == null) player.rPEndPoint = udpReceiveResult.RemoteEndPoint;
                            dataForProcessing.SafeEnqueue(new DataBuffer(udpReceiveResult.Buffer, player, Protocol.Udp));
                            InternalUtils.UpdateStatistics(Statistics.ServerRec, udpReceiveResult.Buffer.Length);
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
        }
        #endregion

        #region Packets
        void PacketProcessing(Player mSender, byte[] buffer, Protocol protocol) //* process packets received from clients.
        {
#if UNITY_SERVER || UNITY_EDITOR
            int length = buffer.Length;
            try
            {
                using (NeutronReader parametersReader = new NeutronReader(buffer))
                {
                    Packet mCommand = parametersReader.ReadPacket<Packet>();
                    switch (mCommand)
                    {
                        case Packet.Connected:
                            HandshakeHandler(mSender, parametersReader.ReadBoolean());
                            break;
                        case Packet.Nickname:
                            NicknameHandler(mSender, parametersReader.ReadString());
                            break;
                        case Packet.Chat:
                            ChatHandler(mSender, parametersReader.ReadPacket<Broadcast>(), parametersReader.ReadString());
                            break;
                        case Packet.Dynamic:
                            DynamicHandler(mSender, parametersReader.ReadPacket<Broadcast>(), parametersReader.ReadPacket<SendTo>(), parametersReader.ReadPacket<CacheMode>(), parametersReader.ReadInt32(), parametersReader.ReadInt32(), parametersReader.ReadExactly(), parametersReader.ReadExactly(), protocol);
                            break;
                        case Packet.NonDynamic:
                            NonDynamicHandler(mSender, parametersReader.ReadInt32(), parametersReader.ReadExactly());
                            break;
                        case Packet.GetChannels:
                            GetChannelsHandler(mSender, mCommand);
                            break;
                        case Packet.JoinChannel:
                            JoinChannelHandler(mSender, mCommand, parametersReader.ReadInt32());
                            break;
                        case Packet.GetChached:
                            GetCacheHandler(mSender, parametersReader.ReadPacket<CachedPacket>(), parametersReader.ReadInt32(), parametersReader.ReadBoolean());
                            break;
                        case Packet.CreateRoom:
                            CreateRoomHandler(mSender, mCommand, parametersReader.ReadString(), parametersReader.ReadInt32(), parametersReader.ReadString(), parametersReader.ReadBoolean(), parametersReader.ReadBoolean(), parametersReader.ReadString());
                            break;
                        case Packet.GetRooms:
                            GetRoomsHandler(mSender, mCommand);
                            break;
                        case Packet.JoinRoom:
                            JoinRoomHandler(mSender, mCommand, parametersReader.ReadInt32());
                            break;
                        case Packet.LeaveRoom:
                            LeaveRoomHandler(mSender, mCommand);
                            break;
                        case Packet.LeaveChannel:
                            LeaveChannelHandler(mSender, mCommand);
                            break;
                        case Packet.DestroyPlayer:
                            DestroyPlayerHandler(mSender, mCommand);
                            break;
                        case Packet.SetPlayerProperties:
                            SetPlayerPropertiesHandler(mSender, parametersReader.ReadString());
                            break;
                        case Packet.SetRoomProperties:
                            SetRoomPropertiesHandler(mSender, parametersReader.ReadString());
                            break;
                        case Packet.Heartbeat:
                            HeartbeatHandler(mSender, parametersReader.ReadDouble());
                            break;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
#endif
        }
        #endregion

        #region MonoBehaviour
        private void Start()
        {
#if UNITY_SERVER
            Console.Clear();
#endif
#if UNITY_EDITOR
            var targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var Api = PlayerSettings.GetApiCompatibilityLevel(targetGroup);
            if (Api != ApiCompatibilityLevel.NET_Standard_2_0)
                Init();
            else NeutronUtils.LoggerError(".NET Standard is not supported, change to .NET 4.x.");
#else
#if !NET_STANDARD_2_0
            Init();
#else
NeutronUtils.LoggerError(".NET Standard is not supported, change to .NET 4.x.");
#endif
#endif
            void Init()
            {
                if (!IsReady)
                    NeutronUtils.LoggerError("The server could not be initialized ):");
                else InitilizeServer();
            }
        }

        private void OnApplicationQuit()
        {
            using (cts)
            {
                //* Disable server(disable all loop and kill all threads).
                cts.Cancel();
                Initialized = false;
                SocketHelper.Dispose();
            }
        }
        #endregion
    }
}
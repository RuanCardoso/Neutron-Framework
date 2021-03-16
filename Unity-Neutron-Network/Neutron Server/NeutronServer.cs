using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server.InternalEvents;
using NeutronNetwork.Internal.Wrappers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Created by: Ruan Cardoso
/// Email: cardoso.ruan050322@gmail.com
/// License: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>

namespace NeutronNetwork.Internal.Server
{
    [RequireComponent(typeof(NeutronConfig))]
    [RequireComponent(typeof(NeutronEvents))]
    [RequireComponent(typeof(NeutronStatistics))]
    public class NeutronServer : NeutronServerFunctions
    {
        #region Events
        //* notifies you if the server has started.
        public static event ServerEvents.OnServerStart onServerStart;
        #endregion

        #region Variables
        //* Signals that the server has been started.
        public static bool Initialized;
        //* generate uniqueID.
        public static int uniqueID = Neutron.generateID;
        //* Amounts of clients that have signed in since the server was started.
        //* This property is not reset and does not decrease its value.
        private static int totalAmountOfPlayers;
        //* all accepted clients will be queued here.
        #endregion

        #region Collections
        private NeutronQueue<TcpClient> acceptedClients = new NeutronQueue<TcpClient>();
        //* here the data received from clients for processing will be queued.
        //* all this processing is done in a single thread on the server, making this whole operation safe for threads.
        private NeutronQueue<DataBuffer> dataForProcessing = new NeutronQueue<DataBuffer>();
        //* [Three Unique Thread] - do not use ThreadPool here.(These methods must have their own dedicated thread for processing.)
        //* Thread pool will join them with other threads that are already processing other methods. causing loss of performance, these must be unique.
        #endregion

        #region Functions
        private void InitilizeServer()
        {
            Initialized = true;
            /////////////////////////////////////////////////////////////////////////////////
            NeutronUtils.Logger("The Server is ready, all protocols have been initialized.\r\n");
            /////////////////////////////////////////////////////////////////////////////////
            Thread acptTh = new Thread((o) => OnAcceptedClient()); //* exclusive thread to accept connections.
            acptTh.Priority = System.Threading.ThreadPriority.Normal;
            acptTh.IsBackground = true;
            acptTh.Start();
            //* This thread processes data received from clients.
            Thread dataForProcessingTh = new Thread((e) =>
            ServerDataProcessingStack());
            dataForProcessingTh.Priority = System.Threading.ThreadPriority.Highest; //* set the max priority.
            dataForProcessingTh.IsBackground = true;
            dataForProcessingTh.Start();
            //* This thread processes the clients in the queue "acceptedClients". 
            Thread stackProcessingAcceptedConnectionsTh = new Thread((e) => AcceptedConnectionsProcessingStack()); // Thread dedicated to processing accepted clients.
            stackProcessingAcceptedConnectionsTh.Priority = System.Threading.ThreadPriority.Normal;
            stackProcessingAcceptedConnectionsTh.IsBackground = true;
            stackProcessingAcceptedConnectionsTh.Start();
            /////////////////////////////////////////////
            onServerStart?.Invoke();
        }
        //* initiates client acceptance.
        private void OnAcceptedClient()
        {
            try
            {
                while (Initialized)
                {
                    TcpClient tcpClient = TcpSocket.AcceptTcpClient();
                    acceptedClients.SafeEnqueue(tcpClient); //* [Thread-Safe]. adds the client to the queue.
                }
            }
            catch (SocketException) { }
        }
        //* start server data processing.
        private void ServerDataProcessingStack()
        {
            while (Initialized) //* infinite loop to keep data processing active.
            {
                dataForProcessing.mEvent.Reset(); //* Sets the state of the event to nonsignaled, which causes threads to block.
                while (dataForProcessing.SafeCount > 0) //* thread-safe - loop to process all data in the queue, before blocking the thread.
                {
                    for (int i = 0; i < NeutronConfig.Settings.ServerSettings.PacketChunkSize && dataForProcessing.SafeCount > 0; i++)
                    {
                        var data = dataForProcessing.SafeDequeue();
                        bool isUDP = (data.protocol == Protocol.Udp) ? true : false;
                        PacketProcessing(data.player, data.buffer, isUDP);
                    }
                }
                dataForProcessing.mEvent.WaitOne(); //* Blocks the current thread until the current WaitHandle receives a signal.
            }
        }
        //* processes the queue clients. [Multiples Thread - ThreadPool for best perfomance.]
        private void AcceptedConnectionsProcessingStack()
        {
            bool SYNCheck(TcpClient synClient)
            {
                string addr = ((IPEndPoint)synClient.Client.RemoteEndPoint).Address.ToString();
                if (addr == IPAddress.Loopback.ToString()) return true;
                if (RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                {
                    if (value > LIMIT_OF_CONNECTIONS_BY_IP)
                    {
                        NeutronUtils.LoggerError("Client not allowed!");
                        synClient.Close();
                        return false;
                    }
                    else
                    {
                        RegisteredConnectionsByIp[addr] = value + 1;
                        return true;
                    }
                }
                else return RegisteredConnectionsByIp.TryAdd(addr, 1);
            }

            while (Initialized)
            {
                acceptedClients.mEvent.Reset(); //* Sets the state of the event to nonsignaled, which causes threads to block.
                while (acceptedClients.SafeCount > 0)
                {
                    var acceptedClient = acceptedClients.SafeDequeue();
                    if (!SYNCheck(acceptedClient)) continue;
                    acceptedClient.NoDelay = NeutronConfig.Settings.GlobalSettings.NoDelay;
                    // TODO acceptedClient.ReceiveTimeout = int.MaxValue;
                    // TODO acceptedClient.SendTimeout = int.MaxValue;
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(); //* Propagates notification that operations should be canceled.
                    Player newPlayer = new Player(Utils.GetUniqueID(), acceptedClient, cancellationTokenSource);
                    if (AddPlayer(newPlayer))
                    {
                        totalAmountOfPlayers++;
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
                    manualResetEvent.Reset();
                    while (queueData.SafeCount > 0)
                    {
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
                                Utils.UpdateStatistics(Statistics.ServerSent, dataLength);
                            }
                        }
                    }
                    manualResetEvent.WaitOne();
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
        }

        private async void OnReceiveData(Player player, Protocol protocol, object toToken)
        {
            CancellationToken token = (CancellationToken)toToken;

            byte[] header = new byte[sizeof(int)];
            byte[] message;

            var netStream = player.tcpClient.GetStream();

            while (Initialized && !token.IsCancellationRequested)
            {
                if (protocol == Protocol.Tcp)
                {
                    if (await Communication.ReadAsyncBytes(netStream, header, 0, sizeof(int), token))
                    {
                        int size = BitConverter.ToInt32(header, 0);
                        if (size > MAX_RECEIVE_MESSAGE_SIZE || size <= 0) HandleDisconnect(player, player._cts);
                        else
                        {
                            message = new byte[size];
                            if (await Communication.ReadAsyncBytes(netStream, message, 0, size, token))
                            {
                                dataForProcessing.SafeEnqueue(new DataBuffer(Protocol.Tcp, message, player));
                                Utils.UpdateStatistics(Statistics.ServerRec, size);
                            }
                            else HandleDisconnect(player, player._cts);
                        }
                    }
                    else HandleDisconnect(player, player._cts);
                }
                else if (protocol == Protocol.Udp)
                {
                    var udpReceiveResult = await player.udpClient.ReceiveAsync();
                    if (udpReceiveResult.Buffer.Length > 0)
                    {
                        if (player.rPEndPoint == null) player.rPEndPoint = udpReceiveResult.RemoteEndPoint;
                        dataForProcessing.SafeEnqueue(new DataBuffer(Protocol.Udp, udpReceiveResult.Buffer, player));
                        Utils.UpdateStatistics(Statistics.ServerRec, udpReceiveResult.Buffer.Length);
                    }
                }
            }
        }
        #endregion

        #region Packets
        void PacketProcessing(Player mSender, byte[] buffer, bool isUDP) //* process packets received from clients.
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
                            HandleConfirmation(mSender, parametersReader.ReadBoolean());
                            break;
                        case Packet.Nickname:
                            HandleNickname(mSender, parametersReader.ReadString());
                            break;
                        case Packet.SendChat:
                            HandleSendChat(mSender, parametersReader.ReadPacket<Broadcast>(), parametersReader.ReadString());
                            break;
                        case Packet.RPC:
                            HandleRPC(mSender, parametersReader.ReadPacket<Broadcast>(), parametersReader.ReadPacket<SendTo>(), parametersReader.ReadInt32(), parametersReader.ReadInt32(), parametersReader.ReadBoolean(), parametersReader.ReadExactly(), parametersReader.ReadExactly(), isUDP);
                            break;
                        case Packet.Static:
                            HandleStatic(mSender, parametersReader.ReadPacket<Broadcast>(), parametersReader.ReadPacket<SendTo>(), parametersReader.ReadInt32(), parametersReader.ReadBoolean(), parametersReader.ReadExactly(), isUDP);
                            break;
                        case Packet.GetChannels:
                            HandleGetChannels(mSender, mCommand);
                            break;
                        case Packet.JoinChannel:
                            HandleJoinChannel(mSender, mCommand, parametersReader.ReadInt32());
                            break;
                        case Packet.GetChached:
                            HandleGetCached(mSender, parametersReader.ReadPacket<CachedPacket>(), parametersReader.ReadInt32());
                            break;
                        case Packet.CreateRoom:
                            HandleCreateRoom(mSender, mCommand, parametersReader.ReadString(), parametersReader.ReadInt32(), parametersReader.ReadString(), parametersReader.ReadBoolean(), parametersReader.ReadBoolean(), parametersReader.ReadString());
                            break;
                        case Packet.GetRooms:
                            HandleGetRooms(mSender, mCommand);
                            break;
                        case Packet.JoinRoom:
                            HandleJoinRoom(mSender, mCommand, parametersReader.ReadInt32());
                            break;
                        case Packet.LeaveRoom:
                            HandleLeaveRoom(mSender, mCommand);
                            break;
                        case Packet.LeaveChannel:
                            HandleLeaveChannel(mSender, mCommand);
                            break;
                        case Packet.DestroyPlayer:
                            HandleDestroyPlayer(mSender, mCommand);
                            break;
                        case Packet.SetPlayerProperties:
                            HandleSetPlayerProperties(mSender, parametersReader.ReadString());
                            break;
                        case Packet.SetRoomProperties:
                            HandleSetRoomProperties(mSender, parametersReader.ReadString());
                            break;
                        case Packet.Heartbeat:
                            HandleHeartbeat(mSender, parametersReader.ReadDouble());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                NeutronUtils.StackTrace(ex);
            }
#endif
        }
        #endregion

        #region MonoBehaviour
#if UNITY_SERVER || UNITY_EDITOR
        private void Start()
        {
#if !NET_STANDARD_2_0
#if !DEVELOPMENT_BUILD
            if (!isReady)
                NeutronUtils.LoggerError("The server could not be initialized):");
            else
            {
                DontDestroyOnLoad(gameObject.transform.root);
                StartCoroutine(Utils.KeepFramerate(NeutronConfig.Settings.ServerSettings.FPS));
                InitilizeServer();
                //NeutronRegister.RegisterSceneObject(null, true, null);
            }
#elif DEVELOPMENT_BUILD
            Console.Clear();
            NeutronUtils.LoggerError("Development build is not supported on the Server.");
#endif
#elif NET_STANDARD_2_0
        Console.Clear();
        NeutronUtils.LoggerError(".NET Standard is not supported, change to .NET 4.x.");
#endif
        }
#endif
        private void OnApplicationQuit()
        {
            Initialized = false; //* Disable server(disable all loop and kill all threads).
            DisposeAllClients(); //* Dispose all client sockets.
            DisposeServerSocket();
            ///////////////////////////////////////////////////////////////
            NeutronUtils.Logger("Server: All resources have been released!!");
            ///////////////////////////////////////////////////////////////
        }
        #endregion
    }
}
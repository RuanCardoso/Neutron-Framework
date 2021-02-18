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
    [RequireComponent(typeof(NeutronEvents))]
    public class NeutronServer : NeutronServerFunctions
    {
        //* Amounts of clients that have signed in since the server was started.
        //* This property is not reset and does not decrease its value.
        static int totalAmountOfPlayers = 0;
        //* notifies you if the server has started.
        public static event SEvents.OnServerStart onServerStart;
        //* all accepted clients will be queued here.
        private NeutronQueue<TcpClient> acceptedClients = new NeutronQueue<TcpClient>();
        //* here the data received from clients for processing will be queued.
        //* all this processing is done in a single thread on the server, making this whole operation safe for threads.
        private NeutronQueue<DataBuffer> dataForProcessing = new NeutronQueue<DataBuffer>();
        //* Signals that the server has been started.
        public static bool Initialized = false;
        //* [Three Unique Thread] - do not use ThreadPool here.(These methods must have their own dedicated thread for processing.)
        //* Thread pool will join them with other threads that are already processing other methods. causing loss of performance, these must be unique.
        private void InitilizeServer()
        {
            Initialized = true;
            /////////////////////////////////////////////////////////////////////////////////
            Utilities.Logger("TCP and UDP have been initialized, the server is ready!\r\n");
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
                    TcpClient tcpClient = ServerSocket.AcceptTcpClient();
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
                dataForProcessing.manualResetEvent.Reset(); //* Sets the state of the event to nonsignaled, which causes threads to block.
                while (dataForProcessing.SafeCount > 0) //* thread-safe - loop to process all data in the queue, before blocking the thread.
                {
                    for (int i = 0; i < IData.serverPacketChunkSize && dataForProcessing.SafeCount > 0; i++)
                    {
                        var data = dataForProcessing.SafeDequeue();
                        bool isUDP = (data.protocol == Protocol.Udp) ? true : false;
                        byte[] bufferToProcess = data.buffer.Decompress((Compression)IData.compressionOptions);
                        PacketProcessing(data.player, bufferToProcess, isUDP);
                    }
                }
                dataForProcessing.manualResetEvent.WaitOne(); //* Blocks the current thread until the current WaitHandle receives a signal.
            }
        }
        //* processes the queue clients. [Multiples Thread - ThreadPool for best perfomance.]
        private void AcceptedConnectionsProcessingStack()
        {
            bool SYNCheck(TcpClient synClient)
            {
                string addr = synClient.RemoteEndPoint().Address.ToString();
                if (addr == IPAddress.Loopback.ToString()) return true;
                if (SYN.TryGetValue(addr, out int value))
                {
                    if (value > LIMIT_OF_CONNECTIONS_BY_IP)
                    {
                        Utilities.LoggerError("Client not allowed!");
                        synClient.Close();
                        return false;
                    }
                    else
                    {
                        SYN[addr] = value + 1;
                        return true;
                    }
                }
                else return SYN.TryAdd(addr, 1);
            }

            while (Initialized)
            {
                acceptedClients.manualResetEvent.Reset(); //* Sets the state of the event to nonsignaled, which causes threads to block.
                while (acceptedClients.SafeCount > 0)
                {
                    var acceptedClient = acceptedClients.SafeDequeue();
                    if (!SYNCheck(acceptedClient)) continue;
                    acceptedClient.NoDelay = IData.serverNoDelay;
                    // TODO acceptedClient.ReceiveTimeout = int.MaxValue;
                    // TODO acceptedClient.SendTimeout = int.MaxValue;
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(); //* Propagates notification that operations should be canceled.
                    Player newPlayer = new Player(Utils.GetUniqueID(acceptedClient.RemoteEndPoint()), acceptedClient, cancellationTokenSource);
                    if (AddPlayer(newPlayer))
                    {
                        totalAmountOfPlayers++;
                        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        Utilities.Logger($"Incoming client, IP: [{acceptedClient.RemoteEndPoint().Address}] | TCP: [{acceptedClient.RemoteEndPoint().Port}] | UDP: [{((IPEndPoint)newPlayer.udpClient.Client.LocalEndPoint).Port}] -:[{totalAmountOfPlayers}]");
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
                acceptedClients.manualResetEvent.WaitOne(); //* Blocks the current thread until the current WaitHandle receives a signal.
            }
        }

        private void OnProcessData(Player player, object toToken)
        {
            try
            {
                CancellationToken token = (CancellationToken)toToken;
                var queueData = player.qData;
                ManualResetEvent manualResetEvent = queueData.manualResetEvent;
                var netStream = player.tcpClient.GetStream();

                while (Initialized && !token.IsCancellationRequested)
                {
                    manualResetEvent.Reset();
                    while (queueData.SafeCount > 0)
                    {
                        for (int i = 0; i < IData.serverProcessChunkSize && queueData.SafeCount > 0; i++)
                        {
                            var data = queueData.SafeDequeue();
                            using (NeutronWriter header = new NeutronWriter())
                            {
                                header.WriteFixedLength(data.buffer.Length); //* write length of message(header).
                                header.Write(data.buffer);
                                byte[] nBuffer = header.ToArray();
                                switch (data.protocol)
                                {
                                    case Protocol.Tcp:
                                        if (player.tcpClient != null)
                                            netStream.Write(nBuffer, 0, nBuffer.Length);
                                        Thread.Sleep(IData.serverSendRate);
                                        break;
                                    case Protocol.Udp:
                                        if (player.rPEndPoint != null && player.tcpClient != null) //* rPEndPointis not thread-safe....  but as it is assigned only once and only by a single thread, it doesn't matter.
                                            player.udpClient.Send(data.buffer, data.buffer.Length, player.rPEndPoint); // send message
                                        Thread.Sleep(IData.serverSendRateUDP);
                                        break;
                                }
                            }
                        }
                    }
                    manualResetEvent.WaitOne();
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex) { Utilities.StackTrace(ex); }
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
                            }
                            else HandleDisconnect(player, player._cts);
                        }
                    }
                    else HandleDisconnect(player, player._cts);
                    ///////////////////////////////////////////
                    await Task.Delay(IData.serverReceiveRate);
                }
                else if (protocol == Protocol.Udp)
                {
                    var udpReceiveResult = await player.udpClient.ReceiveAsync();
                    if (udpReceiveResult.Buffer.Length > 0)
                    {
                        if (player.rPEndPoint == null) player.rPEndPoint = udpReceiveResult.RemoteEndPoint; // remote endpoint to send data. this variable is used in other segments but is only assigned here and only once, it is not necessary to synchronize it (thread-safe) ... I think kkkk
                        dataForProcessing.SafeEnqueue(new DataBuffer(Protocol.Udp, udpReceiveResult.Buffer, player));
                    }
                    //////////////////////////////////////////////
                    await Task.Delay(IData.serverReceiveRateUDP);
                }
            }
        }

        void PacketProcessing(Player mSender, byte[] buffer, bool isUDP) //* process packets received from clients.
        {
#if UNITY_SERVER || UNITY_EDITOR
            int length = buffer.Length;
            try
            {
                using (NeutronReader mReader = new NeutronReader(buffer))
                {
                    Packet mCommand = mReader.ReadPacket<Packet>();
                    switch (mCommand)
                    {
                        case Packet.Connected:
                            HandleConfirmation(mSender, mReader.ReadBoolean());
                            break;
                        case Packet.Nickname:
                            HandleNickname(mSender, mReader.ReadString());
                            break;
                        case Packet.SendChat:
                            HandleSendChat(mSender, mReader.ReadPacket<Broadcast>(), mReader.ReadString());
                            break;
                        case Packet.RPC:
                            HandleRPC(mSender, mReader.ReadPacket<Broadcast>(), mReader.ReadPacket<SendTo>(), mReader.ReadInt32(), mReader.ReadBoolean(), mReader.ReadBytes(length), isUDP);
                            break;
                        case Packet.Static:
                            HandleStatic(mSender, mReader.ReadPacket<Broadcast>(), mReader.ReadPacket<SendTo>(), mReader.ReadInt32(), mReader.ReadBoolean(), mReader.ReadBytes(length), isUDP);
                            break;
                        case Packet.GetChannels:
                            HandleGetChannels(mSender, mCommand);
                            break;
                        case Packet.JoinChannel:
                            HandleJoinChannel(mSender, mCommand, mReader.ReadInt32());
                            break;
                        case Packet.GetChached:
                            HandleGetCached(mSender, mReader.ReadPacket<CachedPacket>(), mReader.ReadInt32());
                            break;
                        case Packet.CreateRoom:
                            HandleCreateRoom(mSender, mCommand, mReader.ReadString(), mReader.ReadInt32(), mReader.ReadString(), mReader.ReadBoolean(), mReader.ReadBoolean(), mReader.ReadString());
                            break;
                        case Packet.GetRooms:
                            HandleGetRooms(mSender, mCommand);
                            break;
                        case Packet.JoinRoom:
                            HandleJoinRoom(mSender, mCommand, mReader.ReadInt32());
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
                            HandleSetPlayerProperties(mSender, mReader.ReadString());
                            break;
                        case Packet.SetRoomProperties:
                            HandleSetRoomProperties(mSender, mReader.ReadString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.StackTrace(ex);
            }
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void Start()
        {
#if !NET_STANDARD_2_0
#if !DEVELOPMENT_BUILD
            if (!_ready)
            {
                Utilities.LoggerError("Failed to initialize server -> error code: 0x1003");
                return;
            }
            if (IData.dontDestroyOnLoad) DontDestroyOnLoad(gameObject.transform.root);
            StartCoroutine(Utils.KeepFramerate(IData.serverFPS));
            InitilizeServer();
#elif DEVELOPMENT_BUILD
        Console.Clear();
        Utilities.Logger("Development build is not supported on the Server.");
#endif
#elif NET_STANDARD_2_0
        Console.Clear();
        Utilities.Logger(".NET Standard is not supported, change to .NET 4.x or IL2CPP.");
#endif
        }
#endif

        private void OnApplicationQuit()
        {
            Initialized = false; //* Disable server(disable all loop and kill all threads).
            DisposeAllClients(); //* Dispose all client sockets.
            Dispose();
            //////////////////////////////////////////////////////
            Utilities.Logger("Server: All resources have been released!!");
            //////////////////////////////////////////////////////
        }
    }
}
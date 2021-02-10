using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server.InternalEvents;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Created by: Ruan Cardoso
/// Email: cardoso.ruan050322@gmail.com
/// License: GNU AFFERO GENERAL PUBLIC LICENSE
/// Third-party libraries: no third-party library was used.
/// </summary>

namespace NeutronNetwork.Internal.Server
{
    [RequireComponent(typeof(NeutronEvents))]
    public class NeutronServer : NeutronSFunc
    {
        public static bool Initialized = false; // Indicates whether the server is started.
        void Initilize()
        {
            //ThreadPool.SetMaxThreads(1000, 1000);
            ///////////////////////////////////////////////////////////////////////////////
            Utils.Logger("TCP and UDP have been initialized, the server is ready!\r\n");
            ///////////////////////////////////////////////////////////////////////////////
            Thread acptTh = new Thread((e) => AcceptClient()); // Thread dedicated to client acceptance.
            acptTh.Priority = System.Threading.ThreadPriority.Highest;
            acptTh.IsBackground = true;
            acptTh.Start();
            Initialized = true; // defines true, server is initialized. [THREAD-SAFE - only accessed by the main thread]
            onServerStart?.Invoke(); // signals that the server has been started. [THREAD-SAFE - delegates are immutable]
        }

        public bool SynFloodProtection(IPAddress address) // check flood connection [THREAD-SAFE - only accessed by the parent thread]
        {
            if (!address.Equals(IPAddress.Loopback))
                return Players.Count(x => x.Value.tcpClient.RemoteEndPoint().Address.Equals(address)) > LIMIT_OF_CONNECTIONS_BY_IP;
            else return false;
        }

        void AcceptClient()
        {
            do
            {
                try
                {
                    TcpClient _clientAccepted = _TCPListen.AcceptTcpClient();
                    _clientAccepted.NoDelay = noDelay; // If true, sends data immediately upon calling NetworkStream.Write. [THREAD-SAFE(NoDelay) - only assigned by the parent thread]

                    CancellationTokenSource _cts = new CancellationTokenSource(); // Signals to a CancellationToken that it should be canceled. to the thread after the client to closed. [THREAD-SAFE - accessed from other threads, microsoft claims to be safe.]

                    IPAddress address = _clientAccepted.RemoteEndPoint().Address; // [THREAD-SAFE - only accessed by the parent thread]

                    if (!blockedConnections.Contains(address)) // check ip its banned; [THREAD-SAFE - only accessed by the parent thread]
                    {
                        if (SynFloodProtection(address)) // flood detected.
                        {
                            _clientAccepted.Dispose(); // dispose attacker socket.
                            _cts.Cancel(); // cancel while thread.
                            _cts.Dispose(); // thread dispose.
                            foreach (var attacker in Players.Where(x => x.Key.RemoteEndPoint().Address.Equals(address))) // get all sockets of attacker and close it. [THREAD-SAFE - is a ConcurrentCollection]
                            {
                                if (Players.TryRemove(attacker.Value.tcpClient, out Player player)) // remove all players of attacker from server. [THREAD-SAFE - is a ConcurrentCollection]
                                {
                                    attacker.Value.tcpClient?.Dispose(); // close socket.
                                    attacker.Value._cts?.Cancel(); // close thread.
                                    attacker.Value._cts?.Dispose();  // thread dispose.
                                }
                            }
                            blockedConnections.Add(address); // add ip to the blacklist. [THREAD-SAFE - only accessed by the parent thread]
                            Utils.LoggerError($"Possible flood attacker! IP Address -> {address}");
                        }
                        else
                        {
                            Player nPlayer = new Player(Utils.GetUniqueID(_clientAccepted.RemoteEndPoint()), _clientAccepted, _cts); // Create new player.
                            if (AddPlayer(nPlayer)) // Add player to the server. [THREAD-SAFE - is a ConcurrentCollection]
                            {
                                IPEndPoint RemoteEndPoint = _clientAccepted.RemoteEndPoint(); // remote endpoint of client.

                                Thread processingStackTh = new Thread(() => ProcessingStack(nPlayer, nPlayer._cts.Token)); // Starts processing client data. data write thread.
                                processingStackTh.Priority = System.Threading.ThreadPriority.BelowNormal;
                                processingStackTh.IsBackground = true;
                                processingStackTh.Start();
                                Thread TcpDataTh = new Thread(() => ReadTCPData(nPlayer, nPlayer._cts.Token)); // Starts processing client data. data write thread.
                                TcpDataTh.Priority = System.Threading.ThreadPriority.BelowNormal;
                                TcpDataTh.IsBackground = true;
                                TcpDataTh.Start();


                                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                Utils.Logger($"Incoming client, IP: [{RemoteEndPoint.Address}] | TCP: [{RemoteEndPoint.Port}] | UDP: [{((IPEndPoint)nPlayer.udpClient.Client.LocalEndPoint).Port}]");
                                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                //ThreadPool.QueueUserWorkItem((e) =>
                                //{
                                //    ReadUDPData(nPlayer, nPlayer._cts.Token); // starts read tcp data. // exclusive UdpClient. each client has its own UDP client.
                                //}); // NOTE: only one thread writes and the other reads the data. [THREAD-SAFE]
                            }
                            else Debug.LogError("failed to add");
                        }
                    }
                    else
                    {
                        _clientAccepted.Dispose(); // close socket;
                        _cts.Cancel(); // close thread.
                        _cts.Dispose(); // thread dispose.
                        Utils.LoggerError($"IP blocked! IP Address -> {address}");
                    }
                }
                catch (ThreadInterruptedException ex) { Utils.StackTrace(ex); }
                catch (ThreadAbortException ex) { Utils.StackTrace(ex); }
                catch (ObjectDisposedException ex) { Utils.StackTrace(ex); }
                catch (Exception ex)
                {
                    if (!Initialized) return;
                    Utils.StackTrace(ex);
                    break;
                }
            } while (Initialized);
        }

        async void ReadTCPData(Player _client, object obj) // server
        {
            byte[] messageLenBuffer = new byte[sizeof(int)]; // receive of packet length.
            byte[] buffer; // message + sizeof(int) + old bytes.
            byte[] releaseBuffer; // only new message.
            try
            {
                CancellationToken token = (CancellationToken)obj; // token for stop thread.

                using (var netStream = _client.tcpClient.GetStream()) // get stream [THREAD SAFE - because only this thread read]
                using (var buffStream = new BufferedStream(netStream, Communication.BUFFER_SIZE))
                {
                    do
                    {
                        if (await Communication.ReadAsyncBytes(buffStream, messageLenBuffer, 0, sizeof(int), token))
                        {
                            int fixedLength = BitConverter.ToInt32(messageLenBuffer, 0); // get message length
                            Utils.LoggerError(fixedLength);
                            if (fixedLength > MAX_RECEIVE_MESSAGE_SIZE || fixedLength <= 0) // check length if =< 0 or message is overflow limit, if true, close socket.
                            {
                                Utils.LoggerError("Operation not allowed, message size is invalid.");
                                HandleDisconnect(_client, _client._cts); // disconnect player. [Thread-Safe]
                            }
                            else
                            {
                                buffer = new byte[fixedLength + sizeof(int)]; // set the buffer size, equal to the packet/message length.
                                if (await Communication.ReadAsyncBytes(buffStream, buffer, sizeof(int), fixedLength, token)) // read message bytes.
                                {
                                    releaseBuffer = new byte[buffer.Length];
                                    Buffer.BlockCopy(buffer, sizeof(int), releaseBuffer, 0, fixedLength);
                                    if (COMPRESSION_MODE == Compression.None) // server
                                        PacketProcessing(_client, releaseBuffer, false); // process packet.
                                    else // server
                                    {
                                        byte[] bBuffer = releaseBuffer.Decompress(COMPRESSION_MODE); // decompress packet.
                                        PacketProcessing(_client, bBuffer, false); // process packet.
                                    }
                                }
                            }
                        }
                        else { HandleDisconnect(_client, _client._cts); } // detects disconnection and closes the socket. [Thread-Safe]
                        await Task.Delay(recRateTCP); // await receive rate.
                    } while (!token.IsCancellationRequested && Initialized);
                }
            }
            catch (ThreadInterruptedException) { }
            catch (ThreadAbortException) { }
            catch (ObjectDisposedException) { } // ignore disposed object log.
            catch (Exception ex)
            {
                Utils.StackTrace(ex); // print stacktrace in console.
                HandleDisconnect(_client, _client._cts); // disconnect client from server. thread safe.
            }
        }

        async void ReadUDPData(Player _owner, object obj) // this client is automatic disposed, based on tcp client.
        {
            try
            {
                CancellationToken token = (CancellationToken)obj; // stop thread token.
                UdpReceiveResult udpReceiveResult; // udp buffer
                do
                {
                    udpReceiveResult = await _owner.udpClient.ReceiveAsync(); // receive bytes
                    if (udpReceiveResult.Buffer.Length > 0)
                    {
                        if (_owner.rPEndPoint == null) _owner.rPEndPoint = udpReceiveResult.RemoteEndPoint; // remote endpoint to send data. this variable is used in other segments but is only assigned here and only once, it is not necessary to synchronize it (thread-safe) ... I think kkkk

                        if (COMPRESSION_MODE == Compression.None) // server
                            PacketProcessing(_owner, udpReceiveResult.Buffer, true); // process packets.
                        else
                        {
                            byte[] bBuffer = udpReceiveResult.Buffer.Decompress(COMPRESSION_MODE); // decompress packet.
                            PacketProcessing(_owner, bBuffer, true); // process packets.
                        }
                    }
                    await Task.Delay(recRateUDP); // await receive rate.
                } while (Initialized && !token.IsCancellationRequested);
            }
            catch (ThreadInterruptedException) { } // ignore
            catch (ThreadAbortException) { } // ignore
            catch (ObjectDisposedException) { } // ignore disposed object log.
            catch (SocketException ex) { Utils.StackTrace(ex); }
        }

        async void ProcessingStack(Player player, object obj) // thread-safe
        {
            CancellationToken token = (CancellationToken)obj;
            var netStream = player.tcpClient.GetStream(); // using statement // stream [Thread-Safe - because only this thread write]
            var buffStream = new BufferedStream(netStream, Communication.BUFFER_SIZE);
            {
                var queueData = player.qData; // data queue for pending processing. [Thread-Safe is a concurrent collection]
                ManualResetEvent manualResetEvent = queueData.manualResetEvent;
                if (queueData != null)
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            manualResetEvent.Reset(); // reset
                            if (queueData.Count > 0)
                            {
                                if (queueData.TryDequeue(out DataBuffer buffer)) // dequeue data.
                                {
                                    using (NeutronWriter header = new NeutronWriter())
                                    {
                                        header.WriteFixedLength(buffer.buffer.Length); // write length of message(header).
                                        header.Write(buffer.buffer); // write message.
                                        byte[] nBuffer = header.ToArray();
                                        switch (buffer.protocol)
                                        {
                                            case Protocol.Tcp:
                                                if (player.tcpClient != null)
                                                    await netStream.WriteAsync(nBuffer, 0, nBuffer.Length, token); // send message.
                                                break;
                                            case Protocol.Udp:
                                                if (player.rPEndPoint != null && player.tcpClient != null) // rPEndPointis not thread-safe....  but as it is assigned only once and only by a single thread, it doesn't matter.
                                                    await player.udpClient.SendAsync(buffer.buffer, buffer.buffer.Length, player.rPEndPoint); // send message
                                                break;
                                        }
                                    }
                                }
                            }
                            manualResetEvent.WaitOne(); // wait for data queued
                        }
                    }
                    catch (ThreadInterruptedException) { } // ignore
                    catch (ThreadAbortException) { } // ignore
                    catch (ObjectDisposedException) { } // ignore disposed object log.
                    catch (Exception ex) { Utils.StackTrace(ex); }
                };
            }
        }

        void PacketProcessing(Player mSender, byte[] buffer, bool isUDP) // process packets received from clients.
        {
            int length = buffer.Length; // length of packet.
            try
            {
                using (NeutronReader mReader = new NeutronReader(buffer)) // read the buffer/message.
                {
                    Packet mCommand = mReader.ReadPacket<Packet>(); // packet.
                    switch (mCommand)
                    {
                        case Packet.Connected:
                            HandleConfirmation(mSender, mReader.ReadBoolean()); // [Thread-Safe]
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
                            HandleRCC(mSender, mReader.ReadPacket<Broadcast>(), mReader.ReadPacket<SendTo>(), mReader.ReadInt32(), mReader.ReadBoolean(), mReader.ReadBytes(length), isUDP);
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
                Utils.StackTrace(ex);
            }
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void Start()
        {
#if !NET_STANDARD_2_0
#if !DEVELOPMENT_BUILD
            if (!_ready) // check server is ready...
            {
                Utils.LoggerError("Failed to initialize server -> error code: 0x1003");
                return;
            }
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject.transform.root);
            StartCoroutine(Utils.KeepFramerate(FPS));
            Initilize();
#elif DEVELOPMENT_BUILD
        Console.Clear();
        Utils.Logger("Development build is not supported on the Server.");
#endif
#elif NET_STANDARD_2_0
        Console.Clear();
        Utils.Logger(".NET Standard is not supported, change to .NET 4.x or IL2CPP.");
#endif
        }
#endif

        /*  void StartUDPVoice () {
            Thread _Thread = new Thread (new ThreadStart (() => {
                _UDPVoiceSocket.localClient.ReceiveBufferSize = 4096; // maximum size of the DGRAM that can be received.
                _UDPVoiceSocket.localClient.SendBufferSize = 4096; // maximum size of the DGRAM that can be sended.
                _UDPVoiceSocket.BeginReceive (OnUDPVoiceReceive, null);
            }));
            _Thread.Start ();
        } */


        private void OnApplicationQuit()
        {
            Initialized = false; // Disable server(disable all loop and kill all threads).
            DisposeAllClients(); // Dispose all client sockets.
            Dispose();
            //////////////////////////////////////////////////////
            Utils.Logger("All resources have been released!!");
            //////////////////////////////////////////////////////
        }
    }
}
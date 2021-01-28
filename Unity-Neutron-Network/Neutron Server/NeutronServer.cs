using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(NeutronEvents))]
public class NeutronServer : NeutronSDatabase
{
    public static bool Initialized = false; // Indicate of server is initialized or not.
    [NonSerialized] public GameObject Container; // Isolates the client and the server. Client and Server have their own container to isolate their objects and functions.
    void Initilize()
    {
        ///////////////////////////////////////////////////////////////////////////////
        Utils.Logger("TCP and UDP have been initialized, the server is ready!\r\n");
        ///////////////////////////////////////////////////////////////////////////////
        ThreadPool.QueueUserWorkItem((e) => AcceptTcpClient()); // thread dedicated to client acceptance.
        //ThreadPool.QueueUserWorkItem((e) => StartUDPVoice ());
        Initialized = true; // defines true, server is initialized.
        onServerStart?.Invoke(); // invoke event.
    }

    async void AcceptTcpClient()
    {
        do
        {
            try
            {
                TcpClient _cAccepted = await _TCPSocket.AcceptTcpClientAsync();
                _cAccepted.NoDelay = noDelay;

                Player nPlayer = new Player(Utils.GetUniqueID(_cAccepted.RemoteEndPoint()), _cAccepted); // Create new player.
                if (AddPlayer(nPlayer)) // Add player to the server. Thread safe
                {
                    IPEndPoint RemoteEndPoint = _cAccepted.RemoteEndPoint(); // End Point of client.

                    ThreadPool.QueueUserWorkItem((e) => ProcessingStack(nPlayer)); // Starts processing client data.
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    Utils.Logger($"Incoming client, IP: [{RemoteEndPoint.Address}] | TCP: [{RemoteEndPoint.Port}] | UDP: [{((IPEndPoint)nPlayer.udpClient.Client.LocalEndPoint).Port}]");
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ThreadPool.QueueUserWorkItem((e) =>
                    {
                        ReadTCPData(nPlayer, nPlayer.buffer); // starts read tcp data.
                        ReadUDPData(nPlayer); // starts read tcp data.
                    });
                }
            }
            catch (Exception ex)
            {
                if (!Initialized) return;
                Utils.LoggerError(ex.Message);
                break;
            }
        } while (Initialized);
    }

    async void ReadTCPData(Player _client, TCPBuffer tcp)
    {
        try
        {
            MessageFraming messageFraming = new MessageFraming();
            int bytesRead = 0;
            do
            {
                bytesRead = await _client.tcpClient.GetStream().ReadAsync(tcp.buffer, 0, TCPBuffer.BUFFER_SIZE); // read data from client.
                if (bytesRead > 0)
                {
                    try
                    {
                        Utils.LoggerError("clientLenght: " + bytesRead);
                        using (NeutronReader neutronReader = new NeutronReader(tcp.buffer, 0, bytesRead))
                        {
                            int prefixedSize = neutronReader.ReadInt32() + sizeof(int);
                            if (messageFraming.lengthOfPacket == -1 && (prefixedSize > 0 && prefixedSize < 65536))
                                messageFraming.lengthOfPacket = prefixedSize;

                            byte[] receivedMessage = neutronReader.ToArray();
                            messageFraming.memoryBuffer.Write(receivedMessage, 0, receivedMessage.Length);
                            messageFraming.offset += bytesRead;
                            Utils.LoggerError($"lop{messageFraming.lengthOfPacket} prefixeS: {prefixedSize}");
                            if ((messageFraming.offset % messageFraming.lengthOfPacket) == 0)
                            {
                                if (messageFraming.offset == messageFraming.lengthOfPacket)
                                {
                                    byte[] fullPckt = messageFraming.memoryBuffer.ToArray();
                                    if (messageFraming.lengthOfPacket == fullPckt.Length)
                                    {
                                        var messages = fullPckt.Split(messageFraming.lengthOfPacket);
                                        foreach (var message in messages)
                                        {
                                            byte[] fullMessage = new byte[message.Length - sizeof(int)];
                                            Buffer.BlockCopy(message, sizeof(int), fullMessage, 0, fullMessage.Length);
                                            byte[] uncompressedMessage = fullMessage.Decompress(compressionMode);
                                            PacketProcessing(_client.tcpClient, uncompressedMessage, false); // process packet
                                        }
                                    }
                                    else Utils.LoggerError("corrupted packet");

                                    messageFraming.lengthOfPacket = -1;
                                    messageFraming.offset = 0;
                                    messageFraming.memoryBuffer = new NeutronWriter();
                                }
                                else Utils.LoggerError("corrupted packet, invalid MOD");
                            }
                        }
                    }
                    catch (Exception ex) { Utils.LoggerError(ex.Message); }
                }
                else HandleDisconnect(_client.tcpClient); // disconnect client from server. thread safe.
            } while (Initialized && bytesRead > 0);
        }
        catch
        {
            if (!Initialized) return;
            HandleDisconnect(_client.tcpClient); // disconnect client from server. thread safe.
        }
    }

    async void ReadUDPData(Player _owner)
    {
        try
        {
            UdpReceiveResult udpReceiveResult; // udp buffer
            do
            {
                udpReceiveResult = await _owner.udpClient.ReceiveAsync(); // receive bytes
                if (udpReceiveResult.Buffer.Length > 0)
                {
                    byte[] decompressedBuffer = udpReceiveResult.Buffer.Decompress(compressionMode);
                    _owner.rPEndPoint = udpReceiveResult.RemoteEndPoint; // remote endpoint to send data.

                    PacketProcessing(_owner.tcpClient, decompressedBuffer, true); // process packets.
                }
            } while (Initialized && udpReceiveResult.Buffer.Length > 0);
        }
        catch (SocketException ex) { Utils.LoggerError(ex.Message); }
    }

    void ProcessingStack(Player player) // thread-safe
    {
        void TCP()
        {
            try
            {
                NetworkStream _stream = player.tcpClient.GetStream(); // stream.
                var queueData = player.qDataTCP; // data queue for pending processing.
                if (queueData != null)
                {
                    queueData.onChanged += async () =>
                    {
                        try
                        {
                            for (int i = 0; i < queueData.Count; i++) // send current and failed packets.
                            {
                                if (queueData.TryDequeue(out byte[] buffer)) // dequeue data.
                                {
                                    using (NeutronWriter writerOnly = new NeutronWriter())
                                    {
                                        writerOnly.Write(buffer.Length); // length of message.
                                        writerOnly.Write(buffer); // message.
                                        byte[] nBuffer = writerOnly.ToArray();
                                        Utils.LoggerError($"serverLenght: {buffer.Length} : " + nBuffer.Length);
                                        if (player.tcpClient != null)
                                            await _stream?.WriteAsync(nBuffer, 0, nBuffer.Length); // send message.
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { Utils.LoggerError("error code: 0x2013 " + ex.Message); }
                    };
                }
            }
            catch (Exception ex) { Utils.LoggerError("error code: 0x2014 " + ex.Message); }
        }

        void UDP()
        {
            try
            {
                var queueData = player.qDataUDP; // data queue for pending processing.
                if (queueData != null)
                {
                    queueData.onChanged += async () =>
                    {
                        try
                        {
                            if (Players.TryGetValue(player.tcpClient, out Player pEndPoint))
                            {
                                for (int i = 0; i < queueData.Count; i++) // send current and failed packets.
                                {
                                    if (queueData.TryDequeue(out byte[] buffer)) // dequeue data.
                                    {
                                        if (pEndPoint.rPEndPoint != null && player.tcpClient != null)
                                            await player.udpClient?.SendAsync(buffer, buffer.Length, pEndPoint.rPEndPoint);
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { Utils.LoggerError("error code: 0x4015 " + ex.Message); }
                    };
                }
            }
            catch (Exception ex) { Utils.LoggerError("error code: 0x4014 " + ex.Message); }
        }
        TCP();
        UDP();
    }

    void PacketProcessing(TcpClient mSocket, byte[] buffer, bool isUDP) // process packets received from clients.
    {
        int length = buffer.Length; // length of packet.
        try
        {
            Player _sender = Players[mSocket]; // who sent...
            using (NeutronReader mReader = new NeutronReader(buffer)) // read the buffer/message.
            {
                Packet mCommand = mReader.ReadPacket<Packet>(); // packet.
                switch (mCommand)
                {
                    case Packet.Connected:
                        HandleConfirmation(_sender, mCommand, mReader.ReadBoolean(), _sender.lPEndPoint);
                        break;
                    case Packet.Nickname:
                        HandleNickname(_sender, mReader.ReadString());
                        break;
                    case Packet.SendChat:
                        HandleSendChat(_sender, mCommand, mReader.ReadPacket<Broadcast>(), mReader.ReadString());
                        break;
                    case Packet.SendInput:
                        HandleSendInput(_sender, mCommand, mReader.ReadBytes(length));
                        break;
                    case Packet.RPC:
                        HandleRPC(_sender, mCommand, mReader.ReadPacket<Broadcast>(), mReader.ReadInt32(), mReader.ReadPacket<SendTo>(), mReader.ReadBoolean(), mReader.ReadBytes(length), isUDP);
                        break;
                    case Packet.RCC:
                        HandleRCC(_sender, mCommand, mReader.ReadPacket<Broadcast>(), mReader.ReadInt32(), mReader.ReadPacket<SendTo>(), mReader.ReadString(), mReader.ReadBoolean(), mReader.ReadBytes(length), isUDP);
                        break;
                    case Packet.OnCustomPacket:
                        HandleOnCustomPacket(_sender, mCommand, mReader.ReadPacket<Packet>(), mReader.ReadBytes(length));
                        break;
                    case Packet.Database:
                        Packet dbPacket = mReader.ReadPacket<Packet>();
                        switch (dbPacket)
                        {
                            case Packet.Login:
                                //if (!isLoggedin(mSocket))
                                {
                                    string username = mReader.ReadString();
                                    string passsword = mReader.ReadString();
                                    new Action(() =>
                                    {
                                        StartCoroutine(Login(_sender, username, passsword));
                                    }).ExecuteOnMainThread();
                                }
                                //else SendErrorMessage(_sender, mCommand, "You are already logged in.");
                                break;
                        }
                        break;
                    case Packet.GetChannels:
                        HandleGetChannels(_sender, mCommand);
                        break;
                    case Packet.JoinChannel:
                        Debug.Log("Joined a channel");
                        HandleJoinChannel(_sender, mCommand, mReader.ReadInt32());
                        break;
                    case Packet.GetChached:
                        HandleGetCached(_sender, mReader.ReadPacket<CachedPacket>(), mReader.ReadInt32());
                        break;
                    case Packet.CreateRoom:
                        HandleCreateRoom(_sender, mCommand, mReader.ReadString(), mReader.ReadInt32(), mReader.ReadString(), mReader.ReadBoolean(), mReader.ReadBoolean(), mReader.ReadString());
                        break;
                    case Packet.GetRooms:
                        HandleGetRooms(_sender, mCommand);
                        break;
                    case Packet.JoinRoom:
                        HandleJoinRoom(_sender, mCommand, mReader.ReadInt32());
                        break;
                    case Packet.LeaveRoom:
                        HandleLeaveRoom(_sender, mCommand);
                        break;
                    case Packet.LeaveChannel:
                        HandleLeaveChannel(_sender, mCommand);
                        break;
                    case Packet.DestroyPlayer:
                        HandleDestroyPlayer(_sender, mCommand);
                        break;
                    case Packet.ServerObjectInstantiate:
                        HandleServerObject(_sender, mReader.ReadPacket<Broadcast>(), mReader.ReadString(), mReader.ReadVector3(), mReader.ReadQuaternion(), mReader.ReadBoolean(), mReader.ReadBytes(length).DeserializeObject<Identity>());
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Utils.LoggerError($"Failed to Response localClient {ex.Message}");
        }
    }

#if UNITY_SERVER || UNITY_EDITOR
    void Start()
    {
#if !NET_STANDARD_2_0
#if !DEVELOPMENT_BUILD
        if (!_ready) // check server is ready...
        {
            Utils.LoggerError("Failed to initialize server -> error code: 0x1003");
            return;
        }
        Container = new GameObject("[Container] -> SERVER");
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
        Dispose(); // Dispose server.
        //////////////////////////////////////////////////////
        Utils.Logger("All resources have been released!!");
        //////////////////////////////////////////////////////
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

public class Neutron : UDPManager
{
    /// <summary>
    /// Returns instance of server
    /// </summary>
    public static NeutronServer Server {
        get {
            if (NeutronSFunc._ != null) return (NeutronServer)NeutronSFunc._;
            Utils.LoggerError("You cannot access server functions on the client outside of Unity Editor.");
            return null;
        }
    }
    /// <summary>
    /// Returns the local Client.
    /// </summary>
    static Neutron _Client = null;
    public static Neutron Client {
        get {
            return _Client;
        }
        set {
            if (_Client == null) _Client = value;
        }
    }
    /// <summary>
    /// Returns true if the player is a bot.
    /// </summary>
    [ReadOnly] public bool isBot = false;
    /// <summary>
    /// Returns the Container.
    /// </summary>
    [ReadOnly] public GameObject Container;
    /// <summary>
    /// Returns the Player Object.
    /// </summary>
    public ClientView ClientView { get; set; }
    /// <summary>
    /// Returns to the local player's instance.
    /// </summary>
    public Player myPlayer;
    /// <summary>
    /// Returns true if connected.
    /// </summary>
    public bool isConnected { get; set; }
    /// <summary>
    /// Get/Set field private.
    /// </summary>
    private string _Nickname;
    /// <summary>
    /// The nickname of the player that will be used to be displayed to other players.
    /// </summary>
    public string Nickname { get => _Nickname; set => SetNickname(value); }
    /// <summary>
    /// This event is called when your connection to the server is established or fails.
    /// </summary>
    public Events.OnNeutronConnected onNeutronConnected { get; set; }
    /// <summary>
    /// This event is triggered when a player is instantiated.
    /// </summary>
    public Events.OnPlayerInstantiated onPlayerInstantiated { get; set; }
    /// <summary>
    /// This event is called when your connection to the server fails.
    /// </summary>
    public Events.OnNeutronDisconnected onNeutronDisconnected { get; set; }
    /// <summary>
    /// This event is called when you receive a message from yourself or other players.
    /// </summary>
    public Events.OnMessageReceived onMessageReceived { get; set; }
    /// <summary>
    /// This function is called when processing database actions on the server.
    /// </summary>
    public Events.OnDatabasePacket onDatabasePacket { get; set; }
    /// <summary>
    /// This event is called after receiving the channel list from the server.
    /// </summary>
    public Events.OnChannelsReceived onChannelsReceived { get; set; }
    /// <summary>
    ///  This event is called after receiving the rooms list from the server.
    /// </summary>
    public Events.OnRoomsReceived onRoomsReceived { get; set; }
    /// <summary>
    /// This event is triggered when your or other players join the channel.
    /// </summary>
    public Events.OnPlayerJoinedChannel onPlayerJoinedChannel { get; set; }
    /// <summary>
    /// This event is triggered when your or other players left the channel.
    /// </summary>
    public Events.OnPlayerLeftChannel onPlayerLeftChannel { get; set; }
    /// <summary>
    /// This event is triggered when your or other players join the room.
    /// </summary>
    public Events.OnPlayerJoinedRoom onPlayerJoinedRoom { get; set; }
    /// <summary>
    /// This event is triggered when your or other players left the room.
    /// </summary>
    public Events.OnPlayerLeftRoom onPlayerLeftRoom { get; set; }
    /// <summary>
    /// This event is triggered when your create a room.
    /// </summary>
    public Events.OnCreatedRoom onCreatedRoom { get; set; }
    /// <summary>
    /// This event is triggered when your get an error.
    /// </summary>
    public Events.OnFailed onFailed { get; set; }
    /// <summary>
    /// This event is triggered when your player is destroyed.
    /// </summary>
    public Events.OnDestroyed onDestroyed { get; set; }
    /// <summary>
    /// This event is triggered when your nickname is changed.
    /// </summary>
    public Events.OnNicknameChanged onNicknameChanged { get; set; }
    /// <summary>
    /// This function will trigger the OnPlayerConnected callback.
    /// Do not call this function in Awake.
    /// </summary>
    /// <param name="ipAddress">The ip of server to connect</param>
    /// <param name="port">The port to connect</param>
    /// <param name="disableInServer">Disable this client on server</param>
    /// <param name="quickPackets">Enable if server quick packets are enabled</param>
    public async void Connect(string ipAddress, bool quickPackets = false)
    {
        if (!isBot)
        {
#if UNITY_SERVER
            Utils.LoggerError($"MainClient disabled in server!\r\n");
            return;
#endif
        }
        else if (isBot)
        {
#if !UNITY_SERVER && !UNITY_EDITOR
            Utils.LoggerError($"Bots disabled in client!");
            return;
#endif
        }

        IData IData = Data.LoadSettings();
        if (IData == null) { Utils.LoggerError("Failed to load settings."); return; }

        Internal();
        //-----------------------------------------------------------------------------------------------------------
        QuickPackets = quickPackets;
        //-----------------------------------------------------------------------------------------------------------
        if (ipAddress.Equals("LocalHost", StringComparison.InvariantCultureIgnoreCase)) ipAddress = "127.0.0.1";
        //-----------------------------------------------------------------------------------------------------------
        _IEPSend = new IPEndPoint(IPAddress.Parse(ipAddress), IData.serverPort);
        //-----------------------------------------------------------------------------------------------------------
        if (!isConnected)
        {
            try
            {
                Utils.Logger("Wait, connecting to the server.");
                //-------------------------------------------------------------------
                await _TCPSocket.ConnectAsync(_IEPSend.Address, _IEPSend.Port);
                //-------------------------------------------------------------------
                if (_TCPSocket.Connected)
                {
                    if (InitConnect()) new Thread(() => TCPListenThread()).Start();
                    //-------------------------------------------------------------------
                    StartUDP();
                    //-------------------------------------------------------------------
                    isConnected = true;
                }
                else if (!_TCPSocket.Connected)
                {
                    Utils.LoggerError("Enable to connect to the server");
                    //-------------------------------------------------------------------
                    onNeutronConnected(false, _);
                }
            }
            catch (SocketException ex)
            {
                Utils.LoggerError($"The connection to the server failed {ex.ErrorCode}");
                //------------------------------------------------------------------------
                onNeutronConnected(false, _);
            }
            EventsProcessor();
        }
        else Utils.LoggerError("Connection Refused!");
    }

    private void EventsProcessor()
    {
        GameObject eObject = new GameObject("EventProcessor");
        eObject.AddComponent<ProcessEvents>();
        //-------------------------------------------------------------------
        ProcessEvents processEvents = eObject.GetComponent<ProcessEvents>();
        //-------------------------------------------------------------------
        processEvents.owner = _;
        processEvents.DPF = 10;
        processEvents.MAO = monoBehaviourActions;
        processEvents.MAT = monoBehaviourRPCActions;
        DontDestroyOnLoad(eObject);
    }

    private void StartUDP()
    {
        Thread _thread = new Thread(new ThreadStart(() =>
        {
            try
            {
                _UDPSocket.BeginReceive(OnUDPReceive, null);
            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }));
        _thread.IsBackground = true;
        _thread.Start();
    }

    private async void TCPListenThread()
    {
        MessageFraming messageFraming = new MessageFraming();
        //using (messageFraming.memoryBuffer)
        {
            do
            {
                if (!_TCPSocket.IsConnected())
                {
                    _TCPSocket.Close();
                }
                else
                {
                    int bytesRead = await _TCPSocket.GetStream().ReadAsync(tcpBuffer.buffer, 0, TCPBuffer.BUFFER_SIZE);
                    if (bytesRead > 0)
                    {
                        try
                        {
                            Utils.LoggerError("clientLenght: " + bytesRead);
                            using (NeutronReader neutronReader = new NeutronReader(tcpBuffer.buffer, 0, bytesRead))
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
                                                byte[] uncompressedMessage = fullMessage.Decompress(COMPRESSION_MODE);
                                                ProcessClientData(uncompressedMessage, uncompressedMessage.Length);
                                            }
                                        }
                                        else Utils.LoggerError("corrupted packet");

                                        messageFraming.lengthOfPacket = -1;
                                        messageFraming.offset = 0;
                                        messageFraming.memoryBuffer = new NeutronWriter();
                                    }
                                    else Utils.LoggerError("corrupted packet, invalid MOD");
                                }


                                //if (hmm > 0) lengthOfPacket = hmm;
                                //Utils.LoggerError($"PacketLenght: {lengthOfPacket} : {offset}");
                                //if ((offset % (lengthOfPacket + sizeof(int))) == 0) // check if packet is completed.
                                //{
                                //    Utils.LoggerError($"competedPacketLenght: {lengthOfPacket} : " + neutronReader.ToArray().Length);
                                //    byte[] arrivedPacket = neutronReader.ToArray(); // Packet completed. copy byte array
                                //    var messages = arrivedPacket.Split(lengthOfPacket + sizeof(int)); // Split packet by length of messages.
                                //    foreach (var message in messages)
                                //    {
                                //        using (NeutronReader neutronMessage = new NeutronReader(message))
                                //        {
                                //            int messageLen = neutronMessage.ReadInt32(); // read length of message.
                                //            Utils.LoggerError($"messageLen: {messageLen}");
                                //            byte[] messageData = neutronMessage.ReadBytes(messageLen); // get the message.
                                //            Utils.LoggerError($"messageData: {messageData.Length}");
                                //            byte[] uncompressedMessage = messageData.Decompress(COMPRESSION_MODE);
                                //            ProcessClientData(uncompressedMessage, uncompressedMessage.Length);
                                //        }
                                //    }
                                //    offset = 0;
                                //}
                            }
                            //var frame = Communication.MessageFraming(tcpBuffer.buffer, bytesRead, COMPRESSION_MODE);
                            //if (frame != null)
                            //{
                            //    foreach (var fr in frame)
                            //    {
                            //        ProcessClientData(fr, fr.Length);
                            //    }
                            //}
                        }
                        catch (Exception ex) { Utils.LoggerError("C434452: " + ex.Message); }
                    }
                    else break;
                }
            } while (_TCPSocket != null);
        }
    }

    public void GetNetworkStats(out long Ping, out double PcktLoss, float delay)
    {
        tNetworkStatsDelay += Time.deltaTime;
        if (tNetworkStatsDelay >= delay)
        {
            pingAmount++;
            using (System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping())
            {
                pingSender.PingCompleted += (object sender, PingCompletedEventArgs e) =>
                {
                    if (e.Reply.Status == IPStatus.Success)
                    {
                        ping = e.Reply.RoundtripTime;
                    }
                    else packetLoss += 1;
                };
                pingSender.SendAsync(_IEPSend.Address, null);
            }
            tNetworkStatsDelay = 0;
        }
        Ping = ping;
        PcktLoss = (packetLoss / pingAmount) * 100;
    }

    public bool isLocalPlayer(Player mPlayer)
    {
        return mPlayer.Equals(myPlayer);
    }
    static int stressPackets = 0;
    private void ProcessClientData(byte[] buffer, int dataLength)
    {
        try
        {
            using (NeutronReader mReader = new NeutronReader(buffer))
            {
                Packet mCommand = mReader.ReadPacket<Packet>();
                switch (mCommand)
                {
                    case Packet.StressTest:
                        stressPackets++;
                        Utils.LoggerError("stressedPackets: " + stressPackets + ": " + mReader.ReadString());
                        break;
                    case Packet.Connected:
                        string playerStatus = mReader.ReadString();
                        int playerUniqueID = mReader.ReadInt32();
                        string playerEndPoint = mReader.ReadString();
                        int playerUDPPort = mReader.ReadInt32();
                        bool playerIsBot = mReader.ReadBoolean();
                        //----------------------------------------------------------------
                        HandleConnected(playerStatus, playerUniqueID, playerIsBot);
                        //----------------------------------------------------------------
                        UDPEndpoint = new IPEndPoint(_IEPSend.Address, playerUDPPort);
                        break;
                    case Packet.DisconnectedByReason:
                        HandleDisconnect(mReader.ReadString());
                        break;
                    case Packet.SendChat:
                        HandleSendChat(mReader.ReadString(), mReader.ReadBytes(dataLength));
                        break;
                    case Packet.SendInput:
                        HandleSendInput(mReader.ReadBytes(dataLength));
                        break;
                    case Packet.RPC:
                        HandleRPC(mReader.ReadInt32(), mReader.ReadBytes(dataLength));
                        break;
                    case Packet.APC:
                        {
                            int ID = mReader.ReadInt32();
                            int playerID = mReader.ReadInt32();
                            byte[] Parameters = mReader.ReadBytes(dataLength);
                            HandleAPC(ID, Parameters, playerID);
                        }
                        break;
                    case Packet.RCC:
                        {
                            string sType = mReader.ReadString();
                            int ID = mReader.ReadInt32();
                            object[] Parameters = mReader.ReadBytes(dataLength).DeserializeObject<object[]>();
                            Player sender = (Player)Parameters[0];
                            HandleRCC(sType, ID, Parameters, false);
                        }
                        break;
                    case Packet.ACC:
                        {
                            int ID = mReader.ReadInt32();
                            string sType = mReader.ReadString();
                            byte[] Parameters = mReader.ReadBytes(dataLength);
                            HandleACC(sType, ID, Parameters);
                        }
                        break;
                    case Packet.Database:
                        Packet dbPacket = mReader.ReadPacket<Packet>();
                        object[] dbResponse = mReader.ReadBytes(dataLength).DeserializeObject<object[]>();
                        HandleDatabase(dbPacket, dbResponse);
                        break;
                    case Packet.GetChannels:
                        int len = mReader.ReadInt32();
                        object[] obj = mReader.ReadBytes(len).DeserializeObject<object[]>();
                        HandleGetChannels((byte[])obj[0]);
                        break;
                    case Packet.JoinChannel:
                        HandleJoinChannel(mReader.ReadBytes(dataLength));
                        break;
                    case Packet.LeaveChannel:
                        HandleLeaveChannel(mReader.ReadBytes(dataLength));
                        break;
                    case Packet.Fail:
                        HandleFail(mReader.ReadPacket<Packet>(), mReader.ReadString());
                        break;
                    case Packet.CreateRoom:
                        Room room = mReader.ReadBytes(dataLength).DeserializeObject<Room>();
                        HandleCreateRoom(room);
                        break;
                    case Packet.GetRooms:
                        Room[] rooms = mReader.ReadBytes(dataLength).DeserializeObject<Room[]>();
                        HandleGetRooms(rooms);
                        break;
                    case Packet.JoinRoom:
                        {
                            byte[] playerJoined = mReader.ReadBytes(dataLength);
                            HandleJoinRoom(playerJoined);
                        }
                        break;
                    case Packet.LeaveRoom:
                        HandleLeaveRoom(mReader.ReadBytes(dataLength));
                        break;
                    case Packet.DestroyPlayer:
                        HandleDestroyPlayer();
                        break;
                    case Packet.Disconnected:
                        HandlePlayerDisconnected(mReader.ReadBytes(dataLength));
                        break;
                    case Packet.SyncBehaviour:
                        HandleJsonProperties(mReader.ReadInt32(), mReader.ReadString());
                        break;
                    case Packet.Nickname:
                        if (onNicknameChanged != null)
                        {
                            new Action(() =>
                            {
                                onNicknameChanged(_);
                            }).ExecuteOnMainThread(this, false);
                        }
                        else Utils.LoggerError("onNicknameChanged event not registered.");
                        break;
                    case Packet.GetChached:
                        {
                            int ownerID = mReader.ReadInt32();
                            Vector3 lastPos = mReader.ReadVector3();
                            Vector3 lastRot = mReader.ReadVector3();
                            byte[] cachedFunc = mReader.ReadBytes(dataLength);
                            object[] newValue = new object[] { lastPos, lastRot };
                            try
                            {
                                properties.AddOrUpdate(ownerID, newValue, (x, y) => newValue);
                                ProcessClientData(cachedFunc, cachedFunc.Length);
                            }
                            catch (Exception ex) { Utils.LoggerError(ex.Message); }
                        }
                        break;
                    case Packet.ServerObjectInstantiate:
                        //string prefName = mReader.ReadString();
                        //Vector3 prefPos = mReader.ReadVector3();
                        //Quaternion prefRot = mReader.ReadQuaternion();
                        //Identity identity = mReader.ReadBytes(dataLength).DeserializeObject<Identity>();
                        //new Action(() =>
                        //{
                        //    NeutronIdentity neutronIdentity = InstantiateServerObject(prefName, prefPos, prefRot, false);
                        //    neutronIdentity.Identity = identity;
                        //}).ExecuteOnMainThread(this, false);
                        break;
                }
            }
        }
        catch (SocketException ex) { Utils.LoggerError(ex.Message + ":" + ex.ErrorCode); }
    }

    //public static void SendVoice (byte[] buffer, int lastPos) {
    //    using (NeutronWriter writer = new NeutronWriter ()) {
    //        writer.WritePacket (Packet.VoiceChat);
    //        writer.Write (lastPos);
    //        writer.Write (buffer);
    //        Send (writer.ToArray(), ProtocolType.Udp);
    //    }
    //}

    public void LeaveRoom()
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.LeaveRoom);
            Send(writer.ToArray());
        }
    }

    public void LeaveChannel()
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.LeaveChannel);
            Send(writer.ToArray());
        }
    }

    public void SendChat(string mMessage, Broadcast broadcast)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.SendChat);
            writer.WritePacket(broadcast);
            writer.Write(mMessage);
            Send(writer.ToArray());
        }
    }

    private void SetNickname(string nickname)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.Nickname);
            writer.Write(nickname);
            Send(writer.ToArray());
        }
        _Nickname = nickname;
    }

    public void JoinChannel(int ChannelID)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.JoinChannel);
            writer.Write(ChannelID);
            Send(writer.ToArray());
        }
    }

    public void JoinRoom(int RoomID)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.JoinRoom);
            writer.Write(RoomID);
            Send(writer.ToArray());
        }
    }

    public void CreateRoom(string roomName, int maxPlayers, string password, Dictionary<string, object> properties, bool visible = true, bool JoinOrCreate = false)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.CreateRoom);
            writer.Write(roomName);
            writer.Write(maxPlayers);
            writer.Write(password ?? string.Empty);
            writer.Write(visible);
            writer.Write(JoinOrCreate);
            writer.Write(JsonConvert.SerializeObject(properties));
            Send(writer.ToArray());
        }
    }

    public void GetCachedPackets(CachedPacket packet, int ID)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.GetChached);
            writer.WritePacket(packet);
            writer.Write(ID);
            Send(writer.ToArray());
        }
    }

    public void GetChannels()
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.GetChannels);
            Send(writer.ToArray());
        }
    }

    public void GetRooms()
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.GetRooms);
            Send(writer.ToArray());
        }
    }

    public void SetPlayerProperties(Dictionary<string, object> properties)
    {
        string _properties = JsonConvert.SerializeObject(properties);
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.SetPlayerProperties);
            writer.Write(_properties);
        }
    }

    /// <summary>
    /// RCC: Creates an instance of this player on the server.
    /// <para>Exclusive RCC ID: 1001</para> 
    /// </summary>
    public void CreatePlayer(MonoBehaviour mThis, NeutronWriter options, SendTo sendTo, Broadcast broadcast)
    {
        RCC(mThis, 1001, options, true, sendTo, ProtocolType.Tcp, broadcast);
    }

    public void DestroyPlayer()
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.DestroyPlayer);
            Send(writer.ToArray());
        }
    }

    public void RCC(MonoBehaviour mThis, int RCCID, NeutronWriter writer, bool enableCache, SendTo sendTo, ProtocolType protocolType, Broadcast broadcast)
    {
        InternalRCC(mThis, RCCID, writer.ToArray(), enableCache, sendTo, protocolType, broadcast);
    }

    private bool RPCTimer(int RPCID, float Delay)
    {
        if (timeRPC.ContainsKey(RPCID))
        {
            timeRPC[RPCID] += Time.deltaTime;
            if (timeRPC[RPCID] >= Delay)
            {
                timeRPC[RPCID] = 0;
                return true;
            }
            return false;
        }
        else
        {
            timeRPC.Add(RPCID, 0);
            return true;
        }
    }

    public void RPC(NeutronBehaviour mThis, int RPCID, float secondsDelay, NeutronWriter parametersStream, SendTo sendTo, bool enableCache, Broadcast broadcast, ProtocolType protocolType = ProtocolType.Tcp)
    {
        if (RPCTimer(RPCID, secondsDelay))
        {
            InternalRPC(mThis, RPCID, new object[] { parametersStream.ToArray() }, sendTo, enableCache, protocolType, broadcast);
        }
    }

    public void SendCustomPacket(object[] parameters, Packet customPacket, ProtocolType protocolType = ProtocolType.Tcp)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.OnCustomPacket);
            writer.WritePacket(customPacket);
            writer.Write(parameters.Serialize());
            switch (protocolType)
            {
                case ProtocolType.Tcp:
                    Send(writer.ToArray(), ProtocolType.Tcp);
                    break;
                case ProtocolType.Udp:
                    Send(writer.ToArray(), ProtocolType.Udp);
                    break;
            }
        }
    }

    /// <summary>
    /// Notifies you of removing an item from the interface.
    /// </summary>
    /// <typeparam name="T">Generic Type</typeparam>
    /// <param name="mParent">Transform parent of interface objects</param>
    /// <param name="mArray">The object that will generate the objects in the interface</param>
    /// <param name="mCacheArray">The list that stores the interface objects generated by mArray</param>
    /// <param name="mDestroy">Indicates whether to remove or destroy the interface object</param>
    /// <returns></returns>
    public bool? NotifyDestroy<T>(Transform mParent, T[] mArray, ref List<T> mCacheArray, IEqualityComparer<T> comparer, bool mDestroy = true) where T : INotify
    {
        if (mParent.childCount > mArray.Length)
        {
            var exceptList = mCacheArray.Except(mArray, comparer);
            int indexRemove = 0;
            foreach (var _ in exceptList.ToList())
            {
                indexRemove = mCacheArray.RemoveAll(x => x.ID == _.ID);
                foreach (Transform _child in mParent)
                {
                    int ID = int.Parse(new string(_child.name.Where(x => char.IsNumber(x)).ToArray()));
                    if (ID == _.ID)
                    {
                        if (mDestroy) Destroy(_child.gameObject);
                        else _child.gameObject.SetActive(false);
                    }
                }
                //if (onNotify) onNotifyDestroy(_.ID, ((NOTIFY)new System.Diagnostics.StackFrame(1).GetMethod().GetCustomAttributes(typeof(NOTIFY), false).FirstOrDefault()).eventName);
            }
            //===========================================================================================
            return Convert.ToBoolean(indexRemove);
        }
        return null;
    }
    /// <summary>
    /// Indicates whether to remove or destroy the interface object
    /// </summary>
    /// <typeparam name="T">Generic Type</typeparam>
    /// <param name="mArray">The list that generated the objects in the interface</param>
    /// <param name="mObject">The expression that will be used to identify whether the objects are the same or not; x => x.ID == ..........</param>
    /// <returns></returns>
    public bool NotifyExist<T>(List<T> mArray, Func<T, bool> mObject)
    {
        return mArray.Any(mObject);
    }
    public void NotifyClear<T>(Transform mParent, List<T> mArray, bool destroy = true)
    {
        foreach (Transform _p in mParent)
        {
            if (destroy) Destroy(_p.gameObject);
            else _p.gameObject.SetActive(false);
            mArray.Clear();
        }
    }
    public void NotifyUpdate<T>(T notify, Transform mParent, Action<Transform, T> method) where T : INotify
    {
        foreach (Transform child in mParent.transform)
        {
            if (child.name == notify.ID.ToString())
            {
                method.Invoke(child, notify);
            }
            else continue;
        }
    }

    public bool isDuplicate(Player player)
    {
        if (neutronObjects.TryGetValue(player.ID, out ClientView obj)) // prevent duplicates in client.
        {
            return true;
        }
        else return false;
    }

    public static Neutron CreateClient(ClientType type, GameObject ownerGameObject)
    {
        Neutron neutronInstance = ownerGameObject.AddComponent<Neutron>();
        neutronInstance.InitializeClient(type);
        if (type == ClientType.MainPlayer) Client = neutronInstance;
        return neutronInstance;
    }

    // Database Manager //

    public void Login(string Username, string Password)
    {
        Send(DBLogin(Username, Password), ProtocolType.Tcp);
    }
}
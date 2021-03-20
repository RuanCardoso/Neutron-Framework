using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork;
using NeutronNetwork.Internal.Server.InternalEvents;
using System.IO;
using System.Threading.Tasks;

namespace NeutronNetwork.Internal.Server
{
    public class NeutronServerFunctions : NeutronServerConstants
    {
        #region Singleton
        public static NeutronServerFunctions _;
        #endregion

        #region Events
        public static event ServerEvents.OnServerAwake onServerAwake;
        public static event ServerEvents.OnPlayerDisconnected onPlayerDisconnected;
        public static event ServerEvents.OnPlayerDestroyed onPlayerDestroyed;
        public static event ServerEvents.OnPlayerJoinedChannel onPlayerJoinedChannel;
        public static event ServerEvents.OnPlayerLeaveChannel onPlayerLeaveChannel;
        public static event ServerEvents.OnPlayerJoinedRoom onPlayerJoinedRoom;
        public static event ServerEvents.OnPlayerLeaveRoom onPlayerLeaveRoom;
        public static event ServerEvents.OnPlayerPropertiesChanged onPlayerPropertiesChanged;
        #endregion

        #region Variables
        public double CurrentTime { get; set; }
        #endregion

        #region MonoBehaviour
        public new void Awake()
        {
            base.Awake();
            void Initialize() => GetComponent<NeutronEvents>().Initialize();
            void WakeUpEvent() => onServerAwake?.Invoke();
            _ = this; //* set this instance.
            Initialize();
            WakeUpEvent();
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void Update()
        {
            CurrentTime = Time.timeAsDouble;
            Utils.ChunkDequeue(ActionsDispatcher, NeutronConfig.Settings.ServerSettings.MonoChunkSize); // process de server data. // [Thread-Safe]
        }
#endif
        #endregion

        #region Functions
        public Player GetPlayer(TcpClient mSocket) => PlayersBySocket[mSocket];
        public Player GetPlayer(int ID) => PlayersById[ID];

        private void DestroyPlayer(Player mPlayer)
        {
            if (mPlayer.NeutronView != null)
            {
                new Action(() =>
                {
                    Destroy(mPlayer.NeutronView.gameObject); // destroy player object.
                }).ExecuteOnMainThread();
            }
        }

        protected bool AddPlayer(Player mPlayer) => PlayersBySocket.TryAdd(mPlayer.tcpClient, mPlayer) && PlayersById.TryAdd(mPlayer.ID, mPlayer);
        private bool RemovePlayer(Player mPlayer)
        {
            void UnsetIpConnection()
            {
                string addr = mPlayer.RemoteEndPoint().Address.ToString();
                if (RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                    RegisteredConnectionsByIp[addr] = --value;
            }

            bool tryRemove = PlayersBySocket.TryRemove(mPlayer.tcpClient, out Player removedPlayerBySocket) && PlayersById.TryRemove(mPlayer.ID, out Player removedPlayerById);
            if (tryRemove)
            {
                UnsetIpConnection();
                DestroyPlayer(mPlayer);
            }
            return tryRemove;
        }

        protected void DisposeAllClients() => PlayersBySocket.ToList().ForEach(x => x.Value?.Dispose());
        protected void DisposeServerSocket() => TcpSocket.Stop();

        private void Cache(int ID, Player owner, byte[] buffer, CachedPacket packet)
        {
            CachedBuffer keyBuffer = new CachedBuffer();
            keyBuffer.ID = ID;
            keyBuffer.owner = owner;
            keyBuffer.buffer = buffer;
            keyBuffer.cachedPacket = packet;

            Channel channel = ChannelsById[owner.CurrentChannel];
            channel.AddCache(keyBuffer);
        }
        #endregion
        public static void SendErrorMessage(Player mSocket, Packet packet, string message) // [Thread-Safe]
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Fail);
                writer.WritePacket(packet);
                writer.Write(message);
                mSocket.Send(SendTo.Only, writer.ToArray(), Broadcast.Only, Protocol.Tcp);
            }
        }

        public static void SendDisconnect(Player mSocket, string reason) // [Thread-Safe]
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.DisconnectedByReason);
                writer.Write(reason);
                mSocket.Send(writer.ToArray());
            }
        }

        private void SendDisconnect(Player mSender) // [Thread-Safe]
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerDisconnected;
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.Disconnected);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
            }
        }
        /// <summary>
        /// Send the response from client or all clients.
        /// </summary>
        /// <param name="mSender">TcpClient of player</param>
        /// <param name="buffer">Message stream</param>
        public static void SocketProtocol(Player mSender, SendTo sendTo, byte[] buffer, Player[] ToSend, bool isUDP) // [Thread-Safe]
        {
            if (ToSend == null) sendTo = SendTo.Only;
            Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
            DataBuffer dataBuffer = new DataBuffer(protocol, buffer);
            switch (sendTo)
            {
                case SendTo.Only:
                    mSender.qData.SafeEnqueue(dataBuffer);
                    break;
                case SendTo.All:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        if (mSender.IsBot)
                            if (!ToSend[i].Equals(mSender) && ToSend[i].IsBot) continue;
                        ToSend[i].qData.SafeEnqueue(dataBuffer);
                    }
                    break;
                case SendTo.Others:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        if (ToSend[i].Equals(mSender)) continue;
                        else if (mSender.IsBot && ToSend[i].IsBot) continue;
                        ToSend[i].qData.SafeEnqueue(dataBuffer);
                    }
                    break;
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Handles
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void HandleDisconnect(Player disconnectedPlayer, CancellationTokenSource cts) // [Thread-Safe]
        {
            using (disconnectedPlayer) // release resources
            using (cts)
            {
                cts.Cancel(); // stop thread
                if (RemovePlayer(disconnectedPlayer)) // remove player from server. [Thread-Safe] 
                    onPlayerDisconnected?.Invoke(disconnectedPlayer); // Thread safe. delegate are immutable.
            }
        }

        protected void HandleConfirmation(Player mSender, bool isBot) // [Thread-Safe]
        {
            mSender.IsBot = isBot;
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize();
                writer.WritePacket(Packet.Connected);
                writer.Write(CurrentTime);
                writer.Write(mSender.lPEndPoint.Port);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                mSender.Send(writer.ToArray());
            }
        }

        protected void HandleNickname(Player mSender, string Nickname)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerNicknameChanged;
            mSender.Nickname = Nickname;
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Nickname);
                writer.WriteExactly<Player>(mSender);
                mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol); // send the message. [Thread-Safe]
            }
        }

        protected void HandleSendChat(Player mSender, Broadcast broadcast, string message) // [Thread-Safe]
        {
            using (NeutronWriter writer = new NeutronWriter()) // write the message.
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.SendChat); // packet name
                writer.Write(message);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                mSender.Send(SendTo.All, writer.ToArray(), broadcast, Protocol.Tcp); // send the message. [Thread-Safe]
            }
        }
        // [Thread-Safe]
        protected void HandleRPC(Player mSender, Broadcast broadcast, SendTo sendMode, int networkObjectId, int rpcID, bool cacheEnabled, byte[] parameters, byte[] infor, bool isUDP = false)
        {
            NeutronMessageInfo MessageInfo = infor.DeserializeObject<NeutronMessageInfo>();
            void Send()
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(Packet.RPC);
                    writer.Write(networkObjectId);
                    writer.Write(rpcID);
                    writer.WriteExactly(parameters);
                    writer.WriteExactly(mSender.Serialize());
                    writer.WriteExactly(MessageInfo.Serialize());

                    Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
                    mSender.Send(sendMode, writer.ToArray(), broadcast, protocol);

                    if (cacheEnabled) Cache(rpcID, mSender, writer.ToArray(), CachedPacket.RPC);
                }
            }

            if (mSender.IsInChannel())
            {
                new Action(() =>
                {
                    if (networkObjectId > 0 && networkObjectId < Neutron.generateID)
                    {
                        Send();
                        return;
                        NeutronView neutronView = networkObjects[networkObjectId];
                        if (neutronView == null) Send();
                        if (Communication.InitRPC(rpcID, parameters, mSender, MessageInfo, neutronView))
                        {
                            Send();
                        }
                    }
                    else
                    {
                        Player findPlayer = GetPlayer(networkObjectId);
                        //if (GetPlayer(playerID, out Player findPlayer))
                        {
                            if (findPlayer.NeutronView == null) Send();
                            else
                            {
                                if (Communication.InitRPC(rpcID, parameters, mSender, MessageInfo, findPlayer.NeutronView))
                                {
                                    Send();
                                }
                            }
                        }
                    };
                }).ExecuteOnMainThread();
            }
            else SendErrorMessage(mSender, Packet.SendChat, "ERROR: You are not on a channel/room.");
        }
        // [Thread-Safe]
        protected void HandleStatic(Player mSender, Broadcast broadcast, SendTo sendMode, int executeID, bool cacheEnabled, byte[] parameters, bool isUDP = false)
        {
            new Action(() =>
            {
                if (Communication.InitRCC(executeID, mSender, parameters, true, null))
                {
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] array = mSender.Serialize();

                        writer.WritePacket(Packet.Static); // packet name
                        writer.Write(executeID);
                        writer.WriteExactly(array);
                        writer.WriteExactly(parameters);

                        Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
                        mSender.Send(sendMode, writer.ToArray(), broadcast, protocol);

                        if (cacheEnabled) Cache(executeID, mSender, writer.ToArray(), CachedPacket.Static);
                    }
                }
            }).ExecuteOnMainThread();
        }
        // [Thread-Safe]
        protected void HandleGetChannels(Player mSender, Packet mCommand)
        {
            try
            {
                if (!mSender.IsInChannel()) // Thread safe.
                {
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] array = ChannelsById.Values.ToArray().Serialize();
                        writer.WritePacket(mCommand);
                        writer.Write(array.Length);
                        writer.Write(array);
                        mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.Only, Protocol.Tcp);
                    }
                }
                else SendErrorMessage(mSender, mCommand, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }
        // [Thread-Safe]
        protected void HandleJoinChannel(Player mSender, Packet mCommand, int channelID)
        {
            // Channels is Thread-Safe because is a ConcurrentDictionary.
            try
            {
                if (ChannelsById.Count == 0) // Thread-Safe - check if count > 0
                {
                    SendErrorMessage(mSender, mCommand, "ERROR: There are no channels created on the server.");
                    return;
                }

                if (ChannelsById.TryGetValue(channelID, out Channel channel)) // Thread-Safe - get channel of ID
                {
                    if (!mSender.IsInChannel())
                    {
                        if (channel.AddPlayer(mSender, out string errorMessage))
                        {
                            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerJoinedChannel;
                            mSender.CurrentChannel = channelID; // Thread safe - because mSender is individual (not simultaneous) - update current channel of player.
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                byte[] array = mSender.Serialize();
                                writer.WritePacket(mCommand); // write packet name.
                                writer.Write(array.Length);
                                writer.Write(array); // writes the player who sent/joined.
                                mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol); // Send request to player.
                            }
                            onPlayerJoinedChannel?.Invoke(mSender); // Thread safe - delegates are immutable. 
                        }
                        else SendErrorMessage(mSender, mCommand, errorMessage);
                    }
                    else SendErrorMessage(mSender, mCommand, "ERROR: You are already joined to a channel.");
                }
                else SendErrorMessage(mSender, mCommand, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }
        // [Thread-Safe]
        protected void HandleCreateRoom(Player mSender, Packet mCommand, string roomName, int maxPlayers, string Password, bool isVisible, bool JoinOrCreate, string options)
        {
            try
            {
                if (mSender.IsInChannel() && !mSender.IsInRoom())
                {
                    Channel channel = ChannelsById[mSender.CurrentChannel];
                    int automaticID = channel.CountOfRooms;
                    Room nRoom = new Room(automaticID, roomName, maxPlayers, !string.IsNullOrEmpty(Password), isVisible, options);

                    void CreateRoom()
                    {
                        if (channel.AddRoom(nRoom, out string errorMessage))
                        {
                            mSender.CurrentRoom = automaticID;
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                byte[] array = nRoom.Serialize();
                                writer.WritePacket(mCommand);
                                writer.WriteExactly(array);
                                mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.Only, Protocol.Tcp);
                            }
                        }
                        else SendErrorMessage(mSender, mCommand, errorMessage);
                    }

                    if (!JoinOrCreate)
                    {
                        CreateRoom();
                    }
                    else
                    {
                        if (!ChannelsById[mSender.CurrentChannel].RoomExists(roomName))
                        {
                            CreateRoom();
                        }
                        else HandleJoinRoom(mSender, Packet.JoinRoom, automaticID);
                    }
                }
                else SendErrorMessage(mSender, mCommand, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }
        // [Thread-Safe]
        protected void HandleGetCached(Player mSender, CachedPacket packetToSendCache, int ID)
        {
            Channel channel = ChannelsById[mSender.CurrentChannel];
            CachedBuffer[] buffers = channel.GetCaches();
            foreach (var cached in buffers)
            {
                CachedBuffer keyBuffer = cached;

                if (keyBuffer.owner.Equals(mSender)) continue;

                if (ID > -1)
                {
                    if (keyBuffer.cachedPacket == packetToSendCache && keyBuffer.ID == ID)
                    {
                        if (ID == 1001)
                        {
                            using (NeutronReader lastReader = new NeutronReader(keyBuffer.buffer))
                            {
                                Packet packet = lastReader.ReadPacket<Packet>();
                                int _ID = lastReader.ReadInt32();
                                byte[] sender = lastReader.ReadExactly();
                                byte[] parameters = lastReader.ReadExactly();

                                using (NeutronWriter oldWriter = new NeutronWriter(new MemoryStream(parameters)))
                                {
                                    NeutronView view = keyBuffer.owner.NeutronView;
                                    if (view != null)
                                    {
                                        oldWriter.Write(view.lastPosition);
                                        oldWriter.Write(view.lastRotation);
                                        parameters = oldWriter.ToArray(); // modified.
                                        using (NeutronWriter writer = new NeutronWriter())
                                        {
                                            writer.WritePacket(packet); // packet name
                                            writer.Write(_ID);
                                            writer.WriteExactly(sender);
                                            writer.WriteExactly(parameters);
                                            mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.Only, Protocol.Tcp);
                                        }
                                    }
                                }
                            }
                        }
                        else mSender.Send(SendTo.Only, keyBuffer.buffer, Broadcast.Only, Protocol.Tcp);
                    }
                    else continue;
                }
                else if (ID == -1)
                {
                    if (keyBuffer.cachedPacket == packetToSendCache)
                    {
                        mSender.Send(SendTo.Only, keyBuffer.buffer, Broadcast.Only, Protocol.Tcp);
                    }
                    else continue;
                }
            }
        }

        protected void HandleGetRooms(Player mSender, Packet mCommand)
        {
            try
            {
                if (mSender.IsInChannel() && !mSender.IsInRoom())
                {
                    Channel channel = ChannelsById[mSender.CurrentChannel];
                    //if (Channels[indexChannel]._rooms.Count == 0) return;
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] array = channel.GetRooms().Serialize();
                        writer.WritePacket(mCommand);
                        writer.Write(array.Length);
                        writer.Write(array);
                        mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.Only, Protocol.Tcp);
                    }
                }
                else SendErrorMessage(mSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }

        protected void HandleLeaveRoom(Player mSender, Packet mCommand)
        {
            if (mSender.IsInRoom())
            {
                var handle = NeutronConfig.Settings.HandleSettings.OnPlayerLeaveRoom;
                using (NeutronWriter writer = new NeutronWriter())
                {
                    byte[] array = mSender.Serialize();
                    writer.WritePacket(mCommand);
                    writer.Write(array.Length);
                    writer.Write(array);
                    mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
                }
                Channel channel = ChannelsById[mSender.CurrentChannel];
                Room room = channel.GetRoom(mSender.CurrentRoom);
                mSender.CurrentRoom = -1;
                room.RemovePlayer(mSender);
            }
            else SendErrorMessage(mSender, mCommand, "ERROR: LeaveRoom Failed");
        }

        protected void HandleLeaveChannel(Player mSender, Packet mCommand)
        {
            if (mSender.IsInChannel())
            {
                var handle = NeutronConfig.Settings.HandleSettings.OnPlayerLeaveChannel;
                using (NeutronWriter writer = new NeutronWriter())
                {
                    byte[] array = mSender.Serialize();
                    writer.WritePacket(mCommand);
                    writer.Write(array.Length);
                    writer.Write(array);
                    mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
                }
                Channel channel = ChannelsById[mSender.CurrentChannel];
                channel.RemovePlayer(mSender);
                mSender.CurrentChannel = -1;
            }
            else SendErrorMessage(mSender, mCommand, "ERROR: LeaveChannel Failed");
        }

        protected void HandleJoinRoom(Player mSender, Packet mCommand, int roomID)
        {
            try
            {
                if (mSender.IsInChannel() && !mSender.IsInRoom())
                {
                    Channel channel = ChannelsById[mSender.CurrentChannel]; // Thread safe
                    Room room = channel.GetRoom(roomID); // thread safe

                    if (room == null) return;

                    if (room.AddPlayer(mSender, out string errorMessage))
                    {
                        var handle = NeutronConfig.Settings.HandleSettings.OnPlayerJoinedRoom;
                        mSender.CurrentRoom = roomID;
                        using (NeutronWriter writer = new NeutronWriter())
                        {
                            byte[] array = mSender.Serialize();
                            writer.WritePacket(mCommand);
                            writer.Write(array.Length);
                            writer.Write(array);
                            mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
                        }
                        onPlayerJoinedRoom?.Invoke(mSender);
                    }
                    else SendErrorMessage(mSender, mCommand, errorMessage);
                }
                else SendErrorMessage(mSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }

        protected void HandleDestroyPlayer(Player mSender, Packet mCommand)
        {
            NeutronView obj = mSender.NeutronView;
            if (obj == null) return;
            new Action(() =>
            {
                var handle = NeutronConfig.Settings.HandleSettings.OnPlayerDestroyed;
                Destroy(obj.gameObject);
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(mCommand);
                    mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
                }
                onPlayerDestroyed?.Invoke(mSender);
            }).ExecuteOnMainThread();
        }

        public void HandleSetPlayerProperties(Player mSender, string properties) // [THREAD-SAFE]
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerPropertiesChanged;
            mSender._ = properties;
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.WriteExactly(arrayBytes);
                mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
            }
        }

        public void HandleSetRoomProperties(Player mSender, string properties) // [THREAD-SAFE]
        {
            if (mSender.IsInRoom())
            {
                Channel channel = ChannelsById[mSender.CurrentChannel];
                Room room = channel.GetRoom(mSender.CurrentRoom);

                if (room.Owner == null)
                {
                    SendErrorMessage(mSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
                    return;
                }

                if (room.Owner.Equals(mSender))
                {
                    var handle = NeutronConfig.Settings.HandleSettings.OnRoomPropertiesChanged;
                    room._ = properties;
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] arrayBytes = mSender.Serialize(); // write the player.
                        writer.WritePacket(Packet.SetRoomProperties);
                        writer.WriteExactly(arrayBytes);
                        mSender.Send(handle.sendTo, writer.ToArray(), handle.broadcast, handle.protocol);
                    }
                }
                else SendErrorMessage(mSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else SendErrorMessage(mSender, Packet.SetRoomProperties, "You are not inside a room.");
        }

        public void HandleHeartbeat(Player mSender, double time)
        {
            double diff = Math.Abs(CurrentTime - time);
            //NeutronUtils.Logger($"diff: {diff} | sT: {serverTime} | cT: {time}");
            if ((int)diff > 0)
            {
                Debug.LogError($"Cara você está dessincronizado irmão : {CurrentTime} : {time}");
                // using (NeutronWriter writer = new NeutronWriter())
                // {
                //     writer.WritePacket(Packet.Heartbeat);
                //     writer.Write(diff);
                //     mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                // }
            }
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Heartbeat);
                mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.Only, Protocol.Udp);
            }
        }
    }
}
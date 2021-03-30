using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork;
using NeutronNetwork.Internal.Server.Delegates;
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
        public static event Events.OnServerAwake onServerAwake;
        public static event Events.OnPlayerDisconnected onPlayerDisconnected;
        public static event Events.OnPlayerDestroyed onPlayerDestroyed;
        public static event Events.OnPlayerJoinedChannel onPlayerJoinedChannel;
        public static event Events.OnPlayerLeaveChannel onPlayerLeaveChannel;
        public static event Events.OnPlayerJoinedRoom onPlayerJoinedRoom;
        public static event Events.OnPlayerLeaveRoom onPlayerLeaveRoom;
        public static event Events.OnPlayerPropertiesChanged onPlayerPropertiesChanged;
        #endregion

        #region Variables
        public double CurrentTime { get; set; }
        #endregion

        #region MonoBehaviour
        public new void Awake()
        {
            base.Awake();
            if (isReady)
            {
                //void Initialize() => GetComponent<NeutronEvents>().Initialize();
                void WakeUpEvent() => onServerAwake.Invoke();
                _ = this; //* set this instance.
                //Initialize();
                WakeUpEvent();
            }
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void Update()
        {
            CurrentTime = Time.timeAsDouble;
            InternalUtils.ChunkDequeue(ActionsDispatcher, NeutronConfig.Settings.ServerSettings.MonoChunkSize); // process de server data. // [Thread-Safe]
        }
#endif
        #endregion

        #region Functions
        public bool GetPlayer(TcpClient nSocket, out Player nPlayer)
        {
            return PlayersBySocket.TryGetValue(nSocket, out nPlayer);
        }

        public bool GetPlayer(int nID, out Player nPlayer)
        {
            return PlayersById.TryGetValue(nID, out nPlayer);
        }

        public bool GetNetworkObject(int nID, Player nPlayer, out NeutronView nView)
        {
            nView = null;
            if (ChannelsById.TryGetValue(nPlayer.CurrentChannel, out Channel nChannel))
            {
                if (nPlayer.IsInRoom())
                {
                    Room Room = nChannel.GetRoom(nPlayer.CurrentRoom);
                    if (Room != null)
                        return Room.sceneSettings.networkObjects.TryGetValue(nID, out nView);
                    else return false;
                }
                else if (nPlayer.IsInChannel())
                    return nChannel.sceneSettings.networkObjects.TryGetValue(nID, out nView);
                else return false;
            }
            else return false;
        }

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
        public static void SocketProtocol(Player mSender, SendTo sendTo, byte[] buffer, Player[] ToSend, bool isUDP)
        {
            Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
            DataBuffer dataBuffer = new DataBuffer(protocol, buffer);
            switch (sendTo)
            {
                case SendTo.Only:
                    if (!mSender.isServer)
                        mSender.qData.SafeEnqueue(dataBuffer);
                    else NeutronUtils.LoggerError("The Server cannot transmit data to itself.");
                    break;
                case SendTo.All:
                    if (ToSend != null)
                    {
                        for (int i = 0; i < ToSend.Length; i++)
                        {
                            if (mSender.IsBot)
                                if (!ToSend[i].Equals(mSender) && ToSend[i].IsBot) continue;
                            ToSend[i].qData.SafeEnqueue(dataBuffer);
                        }
                    }
                    else NeutronUtils.LoggerError("The Server cannot transmit all data to nothing.");
                    break;
                case SendTo.Others:
                    if (ToSend != null)
                    {
                        for (int i = 0; i < ToSend.Length; i++)
                        {
                            if (ToSend[i].Equals(mSender)) continue;
                            else if (mSender.IsBot && ToSend[i].IsBot) continue;
                            ToSend[i].qData.SafeEnqueue(dataBuffer);
                        }
                    }
                    else NeutronUtils.LoggerError("The Server cannot transmit others data to nothing.");
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

        protected void HandleDynamic(Player nSender, Broadcast broadcast, SendTo sendMode, int networkObjectId, int dynamicID, bool isCached, byte[] parameters, byte[] infor, Protocol protocol)
        {
            NeutronMessageInfo NeutronMessageInfo = infor.DeserializeObject<NeutronMessageInfo>();
            void Broadcast(Player mSender)
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(Packet.Dynamic);
                    writer.Write(networkObjectId);
                    writer.Write(dynamicID);
                    writer.WriteExactly(parameters);
                    writer.WriteExactly<Player>(nSender);
                    writer.WriteExactly<NeutronMessageInfo>(NeutronMessageInfo);
                    mSender.Send(sendMode, writer.ToArray(), broadcast, protocol);
                    if (isCached) Cache(dynamicID, nSender, writer.ToArray(), CachedPacket.RPC);
                }
            }

            bool DynamicPlayer(Player nPlayer) => Communication.Dynamic(dynamicID, parameters, nSender, NeutronMessageInfo, nPlayer.NeutronView);
            bool DynamicObject(Player nPlayer, NeutronView nView) => Communication.Dynamic(dynamicID, parameters, nSender, NeutronMessageInfo, nView);

            if (nSender.IsInChannel() || nSender.IsInRoom())
            {
                bool immediateSend = NeutronConfig.Settings.GlobalSettings.SendOnPostProcessing;
                if (InternalUtils.IsSceneObject(networkObjectId))
                {
                    if (GetNetworkObject(networkObjectId, nSender, out NeutronView nView))
                    {
                        new Action(() =>
                        {
                            if (!immediateSend)
                            {
                                Broadcast(nSender);
                                DynamicObject(nSender, nView);
                            }
                            else if (nView != null)
                                if (DynamicObject(nSender, nView))
                                    Broadcast(nSender);
                                else return;
                            else Broadcast(nSender);
                        }).ExecuteOnMainThread();
                    }
                    else Broadcast(nSender);
                }
                else
                {
                    if (GetPlayer(networkObjectId, out Player nPlayer))
                    {
                        new Action(() =>
                        {
                            if (!immediateSend)
                            {
                                Broadcast(nPlayer);
                                DynamicPlayer(nPlayer);
                            }
                            else if (nPlayer.NeutronView != null)
                                if (DynamicPlayer(nPlayer))
                                    Broadcast(nPlayer);
                                else return;
                            else Broadcast(nPlayer);
                        }).ExecuteOnMainThread();
                    }
                    else NeutronUtils.LoggerError("Invalid Network ID, a player with this ID could not be found.");
                }
            }
            else SendErrorMessage(nSender, Packet.Dynamic, "ERROR: You are not on a channel/room.");
        }

        protected void HandleNonDynamic(Player mSender, Broadcast broadcast, SendTo sendMode, int nonDynamicID, bool isCached, byte[] parameters, Protocol protocol)
        {
            void Broadcast()
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    byte[] array = mSender.Serialize();

                    writer.WritePacket(Packet.NonDynamic); // packet name
                    writer.Write(nonDynamicID);
                    writer.WriteExactly(array);
                    writer.WriteExactly(parameters);
                    mSender.Send(sendMode, writer.ToArray(), broadcast, protocol);
                    if (isCached) Cache(nonDynamicID, mSender, writer.ToArray(), CachedPacket.Static);
                }
            }

            bool NonDynamic() => Communication.NonDynamic(nonDynamicID, mSender, parameters, true);

            new Action(() =>
            {
                bool immediateSend = NeutronConfig.Settings.GlobalSettings.SendOnPostProcessing;
                if (immediateSend)
                    if (NonDynamic())
                        Broadcast();
                    else return;
                else
                {
                    Broadcast();
                    NonDynamic();
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
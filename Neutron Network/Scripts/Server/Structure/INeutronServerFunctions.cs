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
        public static NeutronServer _;
        #endregion

        #region Events
        public static event Events.OnServerAwake m_OnAwake;
        public static event Events.OnPlayerDisconnected m_OnPlayerDisconnected;
        public static event Events.OnPlayerDestroyed m_OnPlayerDestroyed;
        public static event Events.OnPlayerJoinedChannel m_OnPlayerJoinedChannel;
        public static event Events.OnPlayerLeaveChannel m_OnPlayerLeaveChannel;
        public static event Events.OnPlayerJoinedRoom m_OnPlayerJoinedRoom;
        public static event Events.OnPlayerLeaveRoom m_OnPlayerLeaveRoom;
        public static event Events.OnPlayerPropertiesChanged m_OnPlayerPropertiesChanged;
        #endregion

        #region Properties
        public double CurrentTime { get; set; }
        #endregion

        #region MonoBehaviour
        public new void Awake()
        {
            base.Awake();
            _ = (NeutronServer)this;
            if (isReady)
                m_OnAwake?.Invoke();
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void Update()
        {
            CurrentTime = (double)Time.unscaledTime;
        }
#endif
        #endregion

        #region Handles
        protected void DisconnectHandle(Player nPlayer)
        {
            using (nPlayer)
            {
                if (SocketHelper.RemovePlayerFromServer(nPlayer))
                    m_OnPlayerDisconnected?.Invoke(nPlayer);
            }
        }

        protected void HandleConfirmation(Player nSender, bool isBot)
        {
            nSender.IsBot = isBot;
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = nSender.Serialize();
                writer.WritePacket(Packet.Connected);
                writer.Write(CurrentTime);
                writer.Write(nSender.lPEndPoint.Port);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                nSender.Send(writer);
            }
        }

        protected void HandleNickname(Player nSender, string Nickname)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerNicknameChanged;
            nSender.Nickname = Nickname;
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Nickname);
                writer.WriteExactly<Player>(nSender);
                nSender.Send(writer, handle);
            }
        }

        protected void HandleSendChat(Player mSender, Broadcast broadcast, string message)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize();
                writer.WritePacket(Packet.SendChat);
                writer.Write(message);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                mSender.Send(writer, SendTo.All, broadcast, Protocol.Tcp);
            }
        }

        protected void HandleDynamic(Player nSender, Broadcast broadcast, SendTo sendTo, CacheMode cacheMode, int networkObjectId, int dynamicID, byte[] parameters, byte[] infor, Protocol protocol)
        {
            NeutronMessageInfo NeutronMessageInfo = infor.DeserializeObject<NeutronMessageInfo>();
            if (nSender.IsInChannel() || nSender.IsInRoom())
            {
                bool immediateSend = NeutronConfig.Settings.GlobalSettings.SendOnPostProcessing;
                if (InternalUtils.IsSceneObject(networkObjectId))
                {
                    if (MatchmakingHelper.GetNetworkObject(networkObjectId, nSender, out NeutronView nView))
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
                        }).DispatchOnMainThread();
                    }
                    else Broadcast(nSender);
                }
                else
                {
                    if (MatchmakingHelper.GetPlayer(networkObjectId, out Player nPlayer))
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
                        }).DispatchOnMainThread();
                    }
                    else NeutronUtils.LoggerError("Invalid Network ID, a player with this ID could not be found.");
                }
            }
            else PlayerHelper.Message(nSender, Packet.Dynamic, "ERROR: You are not on a channel/room.");

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
                    mSender.Send(writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(dynamicID, writer.ToArray(), nSender, cacheMode, CachedPacket.Dynamic);
                }
            }
            bool DynamicPlayer(Player nPlayer) => Communication.Dynamic(dynamicID, parameters, nSender, NeutronMessageInfo, nPlayer.NeutronView);
            bool DynamicObject(Player nPlayer, NeutronView nView) => Communication.Dynamic(dynamicID, parameters, nSender, NeutronMessageInfo, nView);
        }

        protected void HandleNonDynamic(Player mSender, Broadcast broadcast, SendTo sendTo, CacheMode cacheMode, int nonDynamicID, byte[] parameters, Protocol protocol)
        {
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
            }).DispatchOnMainThread();

            void Broadcast()
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    byte[] array = mSender.Serialize();

                    writer.WritePacket(Packet.NonDynamic); // packet name
                    writer.Write(nonDynamicID);
                    writer.WriteExactly(array);
                    writer.WriteExactly(parameters);
                    mSender.Send(writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(nonDynamicID, writer.ToArray(), mSender, cacheMode, CachedPacket.NonDynamic);
                }
            }
            bool NonDynamic() => Communication.NonDynamic(nonDynamicID, mSender, parameters, true);
        }

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
                        mSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(mSender, mCommand, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }
        // [Thread-Safe]
        protected void HandleJoinChannel(Player mSender, Packet mCommand, int channelID)
        {
            // Channels is Thread-Safe because is a ConcurrentDictionary.
            try
            {
                if (ChannelsById.Count == 0)
                {
                    PlayerHelper.Message(mSender, mCommand, "ERROR: There are no channels created on the server.");
                    return;
                }

                if (ChannelsById.TryGetValue(channelID, out Channel channel))
                {
                    if (!mSender.IsInChannel())
                    {
                        if (channel.AddPlayer(mSender))
                        {
                            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerJoinedChannel;
                            mSender.CurrentChannel = channelID;
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                byte[] array = mSender.Serialize();
                                writer.WritePacket(mCommand);
                                writer.Write(array.Length);
                                writer.Write(array);
                                mSender.Send(writer, handle);
                            }
                            m_OnPlayerJoinedChannel?.Invoke(mSender);
                        }
                        else PlayerHelper.Message(mSender, mCommand, "Failed to add Player");
                    }
                    else PlayerHelper.Message(mSender, mCommand, "ERROR: You are already joined to a channel.");
                }
                else PlayerHelper.Message(mSender, mCommand, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
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
                        if (channel.AddRoom(nRoom))
                        {
                            mSender.CurrentRoom = automaticID;
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                byte[] array = nRoom.Serialize();
                                writer.WritePacket(mCommand);
                                writer.WriteExactly(array);
                                mSender.Send(writer);
                            }
                        }
                        else PlayerHelper.Message(mSender, mCommand, "failed create room");
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
                else PlayerHelper.Message(mSender, mCommand, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }
        // [Thread-Safe]
        protected void HandleGetCached(Player nSender, CachedPacket packetToSendCache, int ID, bool includeMe)
        {
            INeutronMatchmaking neutronMatchmaking = MatchmakingHelper.Matchmaking(nSender);
            if (neutronMatchmaking != null)
            {
                foreach (var l_Cache in neutronMatchmaking.GetCaches())
                {
                    if (!includeMe && l_Cache.owner.Equals(nSender)) continue;
                    if (ID > 0)
                    {
                        if (l_Cache.cachedPacket == packetToSendCache && l_Cache.attributeID == ID)
                        {
                            if (ID == 1001 || ID == 1002)
                            {
                                using (NeutronReader lastReader = new NeutronReader(l_Cache.buffer))
                                {
                                    Packet packet = lastReader.ReadPacket<Packet>();
                                    int _ID = lastReader.ReadInt32();
                                    byte[] sender = lastReader.ReadExactly();
                                    byte[] parameters = lastReader.ReadExactly();

                                    using (NeutronWriter oldWriter = new NeutronWriter(new MemoryStream(parameters)))
                                    {
                                        NeutronView view = l_Cache.owner.NeutronView;
                                        if (view != null)
                                        {
                                            oldWriter.Write(view.lastPosition);
                                            oldWriter.Write(view.lastRotation);
                                            parameters = oldWriter.ToArray();
                                            using (NeutronWriter writer = new NeutronWriter())
                                            {
                                                writer.WritePacket(packet);
                                                writer.Write(_ID);
                                                writer.WriteExactly(sender);
                                                writer.WriteExactly(parameters);
                                                nSender.Send(writer);
                                            }
                                        }
                                    }
                                }
                            }
                            else Broadcast(l_Cache.buffer);
                        }
                        else continue;
                    }
                    else if (ID == 0)
                    {
                        if (l_Cache.cachedPacket == packetToSendCache)
                            Broadcast(l_Cache.buffer);
                        else continue;
                    }
                    else if (ID < 0)
                        Broadcast(l_Cache.buffer);
                }
            }

            void Broadcast(byte[] buffer)
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.Write(buffer);
                    nSender.Send(writer);
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
                        mSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(mSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
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
                    mSender.Send(writer, handle);
                }
                Channel channel = ChannelsById[mSender.CurrentChannel];
                Room room = channel.GetRoom(mSender.CurrentRoom);
                mSender.CurrentRoom = -1;
                room.RemovePlayer(mSender);
            }
            else PlayerHelper.Message(mSender, mCommand, "ERROR: LeaveRoom Failed");
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
                    mSender.Send(writer, handle);
                }
                Channel channel = ChannelsById[mSender.CurrentChannel];
                channel.RemovePlayer(mSender);
                mSender.CurrentChannel = -1;
            }
            else PlayerHelper.Message(mSender, mCommand, "ERROR: LeaveChannel Failed");
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

                    if (room.AddPlayer(mSender))
                    {
                        var handle = NeutronConfig.Settings.HandleSettings.OnPlayerJoinedRoom;
                        mSender.CurrentRoom = roomID;
                        using (NeutronWriter writer = new NeutronWriter())
                        {
                            byte[] array = mSender.Serialize();
                            writer.WritePacket(mCommand);
                            writer.Write(array.Length);
                            writer.Write(array);
                            mSender.Send(writer, handle);
                        }
                        m_OnPlayerJoinedRoom?.Invoke(mSender);
                    }
                    else PlayerHelper.Message(mSender, mCommand, "Failed to add player.");
                }
                else PlayerHelper.Message(mSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
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
                    mSender.Send(writer, handle);
                }
                m_OnPlayerDestroyed?.Invoke(mSender);
            }).DispatchOnMainThread();
        }

        public void HandleSetPlayerProperties(Player mSender, string properties)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerPropertiesChanged;
            mSender._ = properties;
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.WriteExactly(arrayBytes);
                mSender.Send(writer, handle);
            }
        }

        public void HandleSetRoomProperties(Player mSender, string properties)
        {
            if (mSender.IsInRoom())
            {
                Channel channel = ChannelsById[mSender.CurrentChannel];
                Room room = channel.GetRoom(mSender.CurrentRoom);

                if (room.Owner == null)
                {
                    PlayerHelper.Message(mSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
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
                        mSender.Send(writer, handle);
                    }
                }
                else PlayerHelper.Message(mSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else PlayerHelper.Message(mSender, Packet.SetRoomProperties, "You are not inside a room.");
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
                mSender.Send(writer, SendTo.Only, Broadcast.Only, Protocol.Udp);
            }
        }
        #endregion
    }
}
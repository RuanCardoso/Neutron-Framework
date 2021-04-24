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
            if (IsReady)
                m_OnAwake?.Invoke();
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void Update() => CurrentTime = (double)Time.unscaledTime;
#endif
        #endregion

        #region Handles
        protected void DisconnectHandler(Player nPlayer)
        {
            using (nPlayer)
            {
                if (SocketHelper.RemovePlayerFromServer(nPlayer))
                    m_OnPlayerDisconnected?.Invoke(nPlayer);
            }
        }

        protected void HandshakeHandler(Player nSender, bool isBot)
        {
            nSender.IsBot = isBot;
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Connected);
                writer.Write(CurrentTime);
                writer.Write(nSender.lPEndPoint.Port);
                writer.WriteExactly<Player>(nSender);
                nSender.Send(writer);
            }
        }

        protected void NicknameHandler(Player nSender, string Nickname)
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

        protected void ChatHandler(Player nSender, Broadcast broadcast, string message)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Chat);
                writer.Write(message);
                writer.WriteExactly<Player>(nSender);
                nSender.Send(writer, SendTo.All, broadcast, Protocol.Tcp);
            }
        }

        protected void DynamicHandler(Player nSender, Broadcast broadcast, SendTo sendTo, CacheMode cacheMode, int networkObjectId, int dynamicID, byte[] parameters, byte[] infor, Protocol protocol)
        {
            #region Logic
            NeutronMessageInfo NeutronMessageInfo = new NeutronMessageInfo(0);
            if (nSender.IsInChannel() || nSender.IsInRoom())
            {
                if (InternalUtils.IsSceneObject(networkObjectId))
                {
                    if (MatchmakingHelper.GetNetworkObject(networkObjectId, nSender, out NeutronView nView))
                    {
                        if (nView.Dynamics.TryGetValue(dynamicID, out RemoteProceduralCall remoteProceduralCall))
                        {
                            Dynamic dynamicAttr = (Dynamic)remoteProceduralCall.attribute;
                            if (dynamicAttr != null)
                            {
                                Action _ = () => { };
                                #region Object Dispatch Logic
                                if (dynamicAttr.SendAfterProcessing)
                                {
                                    _ = new Action(() =>
                                    {
                                        if (DynamicObject(remoteProceduralCall, nView))
                                            Broadcast(nSender);
                                        else return;
                                    });
                                }
                                else
                                {
                                    if (Broadcast(nSender))
                                    {
                                        _ = new Action(() =>
                                        {
                                            DynamicObject(remoteProceduralCall, nView);
                                        });
                                    }
                                }
                                #endregion
                                if (dynamicAttr.DispatchOnMainThread)
                                    NeutronDispatcher.Dispatch(_);
                                else _.Invoke();
                            }
                            else Debug.LogError("Invalid Attribute, there is no valid attribute with this ID.");
                        }
                        else Debug.LogError("Invalid NonDynamic ID, there is no attribute with this ID.");
                    }
                    else Broadcast(nSender);
                }
                else
                {
                    if (MatchmakingHelper.GetPlayer(networkObjectId, out Player nPlayer))
                    {
                        NeutronView neutronView = nPlayer.NeutronView;
                        if (neutronView != null)
                        {
                            if (neutronView.Dynamics.TryGetValue(dynamicID, out RemoteProceduralCall remoteProceduralCall))
                            {
                                Dynamic dynamicAttr = (Dynamic)remoteProceduralCall.attribute;
                                if (dynamicAttr != null)
                                {
                                    Action _ = () => { };
                                    #region Player Dispatch Logic
                                    if (dynamicAttr.SendAfterProcessing)
                                    {
                                        _ = new Action(() =>
                                        {
                                            if (DynamicPlayer(remoteProceduralCall, nPlayer))
                                                Broadcast(nPlayer);
                                            else return;
                                        });
                                    }
                                    else
                                    {
                                        if (Broadcast(nPlayer))
                                        {
                                            _ = new Action(() =>
                                            {
                                                DynamicPlayer(remoteProceduralCall, nPlayer);
                                            });
                                        }
                                    }
                                    #endregion
                                    if (dynamicAttr.DispatchOnMainThread)
                                        NeutronDispatcher.Dispatch(_);
                                    else _.Invoke();
                                }
                                else Debug.LogError("Invalid Attribute, there is no valid attribute with this ID.");
                            }
                            else Debug.LogError("Invalid Dynamic ID, there is no attribute with this ID.");
                        }
                        else Broadcast(nPlayer);
                    }
                    else NeutronUtils.LoggerError("Invalid Network ID, a player with this ID could not be found.");
                }
            }
            else PlayerHelper.Message(nSender, Packet.Dynamic, "ERROR: You are not on a channel/room.");
            #endregion

            #region Local Functions
            bool Broadcast(Player mSender)
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
                return true;
            }
            bool DynamicPlayer(RemoteProceduralCall remoteProceduralCall, Player nPlayer) => Communication.Dynamic(dynamicID, parameters, remoteProceduralCall, nSender, NeutronMessageInfo, nPlayer.NeutronView);
            bool DynamicObject(RemoteProceduralCall remoteProceduralCall, NeutronView nView) => Communication.Dynamic(dynamicID, parameters, remoteProceduralCall, nSender, NeutronMessageInfo, nView);
            #endregion
        }

        protected void NonDynamicHandler(Player nSender, int nonDynamicID, byte[] parameters)
        {
            #region Logic
            if (NeutronNonDynamicBehaviour.NonDynamics.TryGetValue(nonDynamicID, out RemoteProceduralCall remoteProceduralCall))
            {
                NonDynamic nonDynamicAttr = (NonDynamic)remoteProceduralCall.attribute;
                if (nonDynamicAttr != null)
                {
                    Action _ = () => { };
                    #region Dispatch Logic
                    if (nonDynamicAttr.SendAfterProcessing)
                    {
                        _ = new Action(() =>
                        {
                            if (NonDynamic(remoteProceduralCall))
                                Broadcast(nonDynamicAttr.cacheMode, nonDynamicAttr.sendTo, nonDynamicAttr.broadcast, nonDynamicAttr.protocol);
                            else return;
                        });
                    }
                    else
                    {
                        if (Broadcast(nonDynamicAttr.cacheMode, nonDynamicAttr.sendTo, nonDynamicAttr.broadcast, nonDynamicAttr.protocol))
                        {
                            _ = new Action(() =>
                            {
                                NonDynamic(remoteProceduralCall);
                            });
                        }
                    }
                    #endregion
                    if (nonDynamicAttr.DispatchOnMainThread)
                        NeutronDispatcher.Dispatch(_);
                    else _.Invoke();
                }
                else Debug.LogError("Invalid Attribute, there is no valid attribute with this ID.");
            }
            else Debug.LogError("Invalid NonDynamic ID, there is no attribute with this ID.");
            #endregion

            #region Local Functions
            bool Broadcast(CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(Packet.NonDynamic);
                    writer.Write(nonDynamicID);
                    writer.WriteExactly<Player>(nSender);
                    writer.WriteExactly(parameters);
                    nSender.Send(writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(nonDynamicID, writer.ToArray(), nSender, cacheMode, CachedPacket.NonDynamic);
                }
                return true;
            }
            bool NonDynamic(RemoteProceduralCall remoteProceduralCall) => Communication.NonDynamic(nonDynamicID, nSender, parameters, remoteProceduralCall, true);
            #endregion
        }

        protected void GetChannelsHandler(Player nSender, Packet mCommand)
        {
            try
            {
                if (!nSender.IsInChannel())
                {
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        Channel[] channels = ChannelsById.Values.ToArray();
                        writer.WritePacket(mCommand);
                        writer.WriteExactly<Channel[]>(channels);
                        nSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(nSender, mCommand, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }

        protected void JoinChannelHandler(Player nSender, Packet mCommand, int channelID)
        {
            try
            {
                if (ChannelsById.Count == 0)
                {
                    PlayerHelper.Message(nSender, mCommand, "ERROR: There are no channels created on the server.");
                    return;
                }

                if (ChannelsById.TryGetValue(channelID, out Channel channel))
                {
                    if (!nSender.IsInChannel())
                    {
                        if (channel.AddPlayer(nSender))
                        {
                            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerJoinedChannel;
                            nSender.CurrentChannel = channelID;
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                writer.WritePacket(mCommand);
                                writer.WriteExactly<Player>(nSender);
                                nSender.Send(writer, handle);
                            }
                            m_OnPlayerJoinedChannel?.Invoke(nSender);
                        }
                        else PlayerHelper.Message(nSender, mCommand, "Failed to add Player");
                    }
                    else PlayerHelper.Message(nSender, mCommand, "ERROR: You are already joined to a channel.");
                }
                else PlayerHelper.Message(nSender, mCommand, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
        }

        protected void CreateRoomHandler(Player nSender, Packet mCommand, string roomName, int maxPlayers, string Password, bool isVisible, bool JoinOrCreate, string options)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    Channel l_Channel = ChannelsById[nSender.CurrentChannel];
                    int ID = l_Channel.CountOfRooms;
                    Room nRoom = new Room(ID, roomName, maxPlayers, !string.IsNullOrEmpty(Password), isVisible, options);

                    void CreateRoom()
                    {
                        if (l_Channel.AddRoom(nRoom))
                        {
                            nSender.CurrentRoom = ID;
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                writer.WritePacket(mCommand);
                                writer.WriteExactly<Room>(nRoom);
                                nSender.Send(writer);
                            }
                        }
                        else PlayerHelper.Message(nSender, mCommand, "failed create room");
                    }

                    if (!JoinOrCreate)
                    {
                        CreateRoom();
                    }
                    else
                    {
                        if (!ChannelsById[nSender.CurrentChannel].RoomExists(roomName))
                        {
                            CreateRoom();
                        }
                        else JoinRoomHandler(nSender, Packet.JoinRoom, ID);
                    }
                }
                else PlayerHelper.Message(nSender, mCommand, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }

        protected void GetCacheHandler(Player nSender, CachedPacket packetToSendCache, int ID, bool includeMe)
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

        protected void GetRoomsHandler(Player nSender, Packet mCommand)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    Channel channel = ChannelsById[nSender.CurrentChannel];
                    //if (Channels[indexChannel]._rooms.Count == 0) return;
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        Room[] rooms = channel.GetRooms();
                        writer.WritePacket(mCommand);
                        writer.WriteExactly<Room[]>(rooms);
                        nSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(nSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronUtils.LoggerError(ex.Message); }
        }

        protected void LeaveRoomHandler(Player nSender, Packet mCommand)
        {
            if (nSender.IsInRoom())
            {
                var handle = NeutronConfig.Settings.HandleSettings.OnPlayerLeaveRoom;
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(mCommand);
                    writer.WriteExactly<Player>(nSender);
                    nSender.Send(writer, handle);
                }
                INeutronMatchmaking matchmaking = MatchmakingHelper.Matchmaking(nSender);
                if (matchmaking != null)
                {
                    if (matchmaking.RemovePlayer(nSender))
                        MatchmakingHelper.Leave(nSender, leaveChannel: false);
                }
            }
            else PlayerHelper.Message(nSender, mCommand, "ERROR: LeaveRoom Failed");
        }

        protected void LeaveChannelHandler(Player mSender, Packet mCommand)
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

        protected void JoinRoomHandler(Player mSender, Packet mCommand, int roomID)
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

        protected void DestroyPlayerHandler(Player nSender, Packet mCommand)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerDestroyed;
            MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(mCommand);
                nSender.Send(writer, handle);
            }
            m_OnPlayerDestroyed?.Invoke(nSender);
        }

        public void SetPlayerPropertiesHandler(Player mSender, string properties)
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

        public void SetRoomPropertiesHandler(Player mSender, string properties)
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

        public void HeartbeatHandler(Player mSender, double time)
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
                mSender.Send(writer, SendTo.Me, Broadcast.Me, Protocol.Udp);
            }
        }
        #endregion
    }
}
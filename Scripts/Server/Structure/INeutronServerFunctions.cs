using System;
using System.Linq;
using UnityEngine;
using NeutronNetwork.Internal.Server.Delegates;
using System.IO;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;

namespace NeutronNetwork.Server
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

        protected void HandshakeHandler(Player nSender)
        {
            #region Handshake
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Handshake);
                writer.Write(CurrentTime);
                writer.Write(nSender.lPEndPoint.Port);
                writer.WriteExactly<Player>(nSender);
                writer.WriteExactly<Player[]>(PlayersBySocket.Values.ToArray()); // send the other players to me.
                nSender.Send(writer);
            }
            #endregion

            #region Players
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.NewPlayer);
                writer.WriteExactly<Player>(nSender); // send me to the other players who were already connected.
                nSender.Send(writer, SendTo.Others, Broadcast.Server, Protocol.Tcp);
            }
            #endregion
        }

        protected void NicknameHandler(Player nSender, string Nickname)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerNicknameChanged;
            nSender.Nickname = Nickname;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Nickname);
                writer.Write(nSender.ID);
                nSender.Send(writer, handle);
            }
        }

        protected void ChatHandler(Player nSender, ChatPacket chatPacket, Broadcast broadcast, int networkID, string message)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Chat);
                writer.Write(message);
                writer.Write(nSender.ID);

                if (chatPacket == ChatPacket.Global)
                    nSender.Send(writer, SendTo.All, broadcast, Protocol.Tcp);
                else if (chatPacket == ChatPacket.Private)
                {
                    if (MatchmakingHelper.GetPlayer(networkID, out Player nPlayer))
                    {
                        nPlayer.Send(writer, SendTo.Me, Broadcast.Me, Protocol.Tcp);
                    }
                    else PlayerHelper.Message(nSender, SystemPacket.Chat, "Player not found.");
                }
            }
        }

        protected void iRPCHandler(Player nSender, Broadcast broadcast, SendTo sendTo, CacheMode cacheMode, int networkID, int attributeID, byte[] parameters, Protocol nRecProtocol, Protocol protocol)
        {
            #region Logic
            if (nSender.IsInChannel() || nSender.IsInRoom())
            {
                if (SceneHelper.IsSceneObject(networkID))
                {
                    if (MatchmakingHelper.GetNetworkObject(networkID, nSender, out NeutronView nView))
                    {
                        if (nView.iRPCs.TryGetValue(attributeID, out RemoteProceduralCall remoteProceduralCall))
                        {
                            iRPC dynamicAttr = (iRPC)remoteProceduralCall.attribute;
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
                        else Debug.LogError("Invalid gRPC ID, there is no attribute with this ID.");
                    }
                    else Broadcast(nSender);
                }
                else
                {
                    if (MatchmakingHelper.GetPlayer(networkID, out Player nPlayer))
                    {
                        NeutronView neutronView = nPlayer.NeutronView;
                        if (neutronView != null)
                        {
                            if (neutronView.iRPCs.TryGetValue(attributeID, out RemoteProceduralCall remoteProceduralCall))
                            {
                                iRPC dynamicAttr = (iRPC)remoteProceduralCall.attribute;
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
                            else Debug.LogError("Invalid iRPC ID, there is no attribute with this ID.");
                        }
                        else Broadcast(nPlayer);
                    }
                    else NeutronLogger.LoggerError("Invalid Network ID, a player with this ID could not be found.");
                }
            }
            else PlayerHelper.Message(nSender, SystemPacket.iRPC, "ERROR: You are not on a channel/room.");
            #endregion

            #region Local Functions
            bool Broadcast(Player mSender)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(SystemPacket.iRPC);
                    writer.Write(networkID);
                    writer.Write(attributeID);
                    writer.Write(nSender.ID);
                    writer.WriteExactly(parameters);
                    mSender.Send(writer, sendTo, broadcast, nRecProtocol);
                    MatchmakingHelper.SetCache(attributeID, writer.ToArray(), nSender, cacheMode, CachedPacket.iRPC);
                }
                return true;
            }
            bool DynamicPlayer(RemoteProceduralCall remoteProceduralCall, Player nPlayer) => NeutronHelper.iRPC(parameters, false, remoteProceduralCall, nSender, nPlayer.NeutronView);
            bool DynamicObject(RemoteProceduralCall remoteProceduralCall, NeutronView nView) => NeutronHelper.iRPC(parameters, false, remoteProceduralCall, nSender, nView);
            #endregion
        }

        protected void gRPCHandler(Player nSender, int networkID, int nonDynamicID, byte[] parameters)
        {
            #region Logic
            if (MatchmakingHelper.GetPlayer(networkID, out Player nPlayer))
            {
                if (NeutronNonDynamicBehaviour.gRPCs.TryGetValue(nonDynamicID, out RemoteProceduralCall remoteProceduralCall))
                {
                    gRPC nonDynamicAttr = (gRPC)remoteProceduralCall.attribute;
                    if (nonDynamicAttr != null)
                    {
                        Action _ = () => { };
                        #region Dispatch Logic
                        if (nonDynamicAttr.SendAfterProcessing)
                        {
                            _ = new Action(() =>
                            {
                                if (gRPC(remoteProceduralCall))
                                    Broadcast(nPlayer, nonDynamicAttr.cacheMode, nonDynamicAttr.sendTo, nonDynamicAttr.broadcast, nonDynamicAttr.protocol);
                                else return;
                            });
                        }
                        else
                        {
                            if (Broadcast(nPlayer, nonDynamicAttr.cacheMode, nonDynamicAttr.sendTo, nonDynamicAttr.broadcast, nonDynamicAttr.protocol))
                            {
                                _ = new Action(() =>
                                {
                                    gRPC(remoteProceduralCall);
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
                else Debug.LogError("Invalid gRPC ID, there is no attribute with this ID.");
            }
            #endregion

            #region Local Functions
            bool Broadcast(Player mSender, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(SystemPacket.gRPC);
                    writer.Write(nonDynamicID);
                    writer.Write(nSender.ID);
                    writer.WriteExactly(parameters);
                    mSender.Send(writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(nonDynamicID, writer.ToArray(), nSender, cacheMode, CachedPacket.gRPC);
                }
                return true;
            }
            bool gRPC(RemoteProceduralCall remoteProceduralCall) => NeutronHelper.gRPC(nonDynamicID, nSender, parameters, remoteProceduralCall, true, false);
            #endregion
        }

        protected void GetChannelsHandler(Player nSender)
        {
            try
            {
                if (!nSender.IsInChannel())
                {
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.SetLength(0);
                        Channel[] channels = ChannelsById.Values.ToArray();
                        writer.WritePacket(SystemPacket.GetChannels);
                        writer.WriteExactly<Channel[]>(channels);
                        nSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(nSender, SystemPacket.GetChannels, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronLogger.LoggerError(ex.Message); }
        }

        protected void JoinChannelHandler(Player nSender, int channelID)
        {
            try
            {
                if (ChannelsById.Count == 0)
                {
                    PlayerHelper.Message(nSender, SystemPacket.JoinChannel, "ERROR: There are no channels created on the server.");
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
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.SetLength(0);
                                writer.WritePacket(SystemPacket.JoinChannel);
                                writer.Write(nSender.ID);
                                nSender.Send(writer, handle);
                            }
                            nSender.Matchmaking = MatchmakingHelper.Matchmaking(nSender);
                            m_OnPlayerJoinedChannel?.Invoke(nSender);
                        }
                        else PlayerHelper.Message(nSender, SystemPacket.JoinChannel, "Failed to add Player");
                    }
                    else PlayerHelper.Message(nSender, SystemPacket.JoinChannel, "ERROR: You are already joined to a channel.");
                }
                else PlayerHelper.Message(nSender, SystemPacket.JoinChannel, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { NeutronLogger.StackTrace(ex); }
        }

        protected void CreateRoomHandler(Player nSender, string roomName, int maxPlayers, string Password, bool isVisible, bool JoinOrCreate, string options)
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
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.SetLength(0);
                                writer.WritePacket(SystemPacket.CreateRoom);
                                writer.WriteExactly<Room>(nRoom);
                                nSender.Send(writer);
                            }
                        }
                        else PlayerHelper.Message(nSender, SystemPacket.CreateRoom, "failed create room");
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
                        else JoinRoomHandler(nSender, ID);
                    }
                }
                else PlayerHelper.Message(nSender, SystemPacket.CreateRoom, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { NeutronLogger.LoggerError(ex.Message); }
        }

        protected void GetCacheHandler(Player nSender, CachedPacket packetToSendCache, int ID, bool includeMe)
        {
            INeutronMatchmaking neutronMatchmaking = nSender.Matchmaking;
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
                                #region Pool
                                var lastReader = Neutron.PooledNetworkReaders.Pull();
                                lastReader.SetLength(0);
                                lastReader.SetBuffer(l_Cache.buffer);
                                lastReader.SetPosition(0);
                                #endregion
                                //using (NeutronReader lastReader = new NeutronReader(l_Cache.buffer))
                                {
                                    SystemPacket packet = lastReader.ReadPacket<SystemPacket>();
                                    int _ID = lastReader.ReadInt32();
                                    byte[] sender = lastReader.ReadExactly();
                                    byte[] parameters = lastReader.ReadExactly();

                                    using (NeutronWriter oldWriter = new NeutronWriter(new MemoryStream(parameters)))
                                    {
                                        NeutronView view = l_Cache.owner.NeutronView;
                                        if (view != null)
                                        {
                                            oldWriter.Write(view.LastPosition);
                                            oldWriter.Write(view.LastRotation);
                                            parameters = oldWriter.ToArray();
                                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                                            {
                                                writer.SetLength(0);
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
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.Write(buffer);
                    nSender.Send(writer);
                }
            }
        }

        protected void GetRoomsHandler(Player nSender)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    Channel channel = ChannelsById[nSender.CurrentChannel];
                    //if (Channels[indexChannel]._rooms.Count == 0) return;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.SetLength(0);
                        Room[] rooms = channel.GetRooms();
                        writer.WritePacket(SystemPacket.GetRooms);
                        writer.WriteExactly<Room[]>(rooms);
                        nSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(nSender, SystemPacket.GetRooms, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronLogger.LoggerError(ex.Message); }
        }

        protected void LeaveRoomHandler(Player nSender)
        {
            if (nSender.IsInRoom())
            {
                var handle = NeutronConfig.Settings.HandleSettings.OnPlayerLeaveRoom;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(SystemPacket.Leave);
                    writer.WritePacket(MatchmakingPacket.Room);
                    writer.Write(nSender.ID);
                    nSender.Send(writer, handle);
                }
                INeutronMatchmaking matchmaking = nSender.Matchmaking;
                if (matchmaking != null)
                {
                    if (matchmaking.RemovePlayer(nSender))
                        MatchmakingHelper.Leave(nSender, leaveChannel: false);
                }
            }
            else PlayerHelper.Message(nSender, SystemPacket.Leave, "ERROR: LeaveRoom Failed");
        }

        protected void LeaveChannelHandler(Player nSender)
        {
            if (nSender.IsInChannel())
            {
                var handle = NeutronConfig.Settings.HandleSettings.OnPlayerLeaveChannel;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(SystemPacket.Leave);
                    writer.WritePacket(MatchmakingPacket.Channel);
                    writer.Write(nSender.ID);
                    nSender.Send(writer, handle);
                }
                Channel channel = ChannelsById[nSender.CurrentChannel];
                channel.RemovePlayer(nSender);
                nSender.CurrentChannel = -1;
            }
            else PlayerHelper.Message(nSender, SystemPacket.Leave, "ERROR: LeaveChannel Failed");
        }

        protected void JoinRoomHandler(Player nSender, int roomID)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    Channel channel = ChannelsById[nSender.CurrentChannel]; // Thread safe
                    Room room = channel.GetRoom(roomID); // thread safe

                    if (room == null) return;

                    if (room.AddPlayer(nSender))
                    {
                        var handle = NeutronConfig.Settings.HandleSettings.OnPlayerJoinedRoom;
                        nSender.CurrentRoom = roomID;
                        using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                        {
                            writer.SetLength(0);
                            writer.WritePacket(SystemPacket.JoinRoom);
                            writer.Write(nSender.ID);
                            nSender.Send(writer, handle);
                        }
                        nSender.Matchmaking = MatchmakingHelper.Matchmaking(nSender);
                        m_OnPlayerJoinedRoom?.Invoke(nSender);
                    }
                    else PlayerHelper.Message(nSender, SystemPacket.JoinRoom, "Failed to add player.");
                }
                else PlayerHelper.Message(nSender, SystemPacket.JoinRoom, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { NeutronLogger.LoggerError(ex.Message); }
        }

        protected void DestroyPlayerHandler(Player nSender)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerDestroyed;
            MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.DestroyPlayer);
                nSender.Send(writer, handle);
            }
            m_OnPlayerDestroyed?.Invoke(nSender);
        }

        public void SetPlayerPropertiesHandler(Player nSender, string properties)
        {
            var handle = NeutronConfig.Settings.HandleSettings.OnPlayerPropertiesChanged;
            nSender._ = properties;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.SetPlayerProperties);
                writer.Write(nSender.ID);
                nSender.Send(writer, handle);
            }
        }

        public void SetRoomPropertiesHandler(Player nSender, string properties)
        {
            if (nSender.IsInRoom())
            {
                Channel channel = ChannelsById[nSender.CurrentChannel];
                Room room = channel.GetRoom(nSender.CurrentRoom);

                if (room.Owner == null)
                {
                    PlayerHelper.Message(nSender, SystemPacket.SetRoomProperties, "You are not allowed to change the properties of this room.");
                    return;
                }

                if (room.Owner.Equals(nSender))
                {
                    var handle = NeutronConfig.Settings.HandleSettings.OnRoomPropertiesChanged;
                    room._ = properties;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.SetLength(0);
                        writer.WritePacket(SystemPacket.SetRoomProperties);
                        writer.Write(nSender.ID);
                        nSender.Send(writer, handle);
                    }
                }
                else PlayerHelper.Message(nSender, SystemPacket.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else PlayerHelper.Message(nSender, SystemPacket.SetRoomProperties, "You are not inside a room.");
        }

        public void HeartbeatHandler(Player mSender, double time)
        {
            double diff = Math.Abs(CurrentTime - time);
            NeutronLogger.Logger($"diff: {diff} | sT: {CurrentTime} | cT: {time}");
            if ((int)diff > 0)
            {
                //Debug.LogError($"Cara você está dessincronizado irmão : {CurrentTime} : {time}");
                // using (NeutronWriter writer = new NeutronWriter())
                // {
                //     writer.WritePacket(Packet.Heartbeat);
                //     writer.Write(diff);
                //     mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                // }
            }
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Heartbeat);
                mSender.Send(writer, SendTo.Me, Broadcast.Me, Protocol.Udp);
            }
        }

        public void ClientPacketHandler(Player nSender, bool isMine, int networkID, byte[] parameters, ClientPacket clientPacket, SendTo sendTo, Broadcast broadcast, Protocol recProtocol)
        {
            if (MatchmakingHelper.GetPlayer(networkID, out Player nPlayer))
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0);
                    nWriter.WritePacket(SystemPacket.ClientPacket);
                    nWriter.WritePacket(clientPacket);
                    nWriter.Write(nSender.ID);
                    nWriter.WriteExactly(parameters);

                    if (isMine)
                        nPlayer.Send(nWriter, sendTo, broadcast, recProtocol);
                    else nPlayer.Send(nWriter, SendTo.Me, Broadcast.Me, recProtocol);
                }
            }
            else Debug.LogError("dsdsdsd");
        }

        public void OnSerializeViewHandler(Player nSender, int networkID, int instanceID, byte[] parameters, SendTo sendTo, Broadcast broadcast, Protocol recProtocol)
        {
            if (SceneHelper.IsSceneObject(networkID))
            {
                if (MatchmakingHelper.GetNetworkObject(networkID, nSender, out NeutronView nView))
                {
                    if (nView.NBs.TryGetValue(instanceID, out NeutronBehaviour neutronBehaviour))
                    {
                        NeutronDispatcher.Dispatch(() =>
                        {
                            using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                            {
                                nReader.SetBuffer(parameters);
                                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                                {
                                    nWriter.SetLength(0);
                                    {
                                        if (neutronBehaviour.OnNeutronSerializeView(nWriter, nReader, false))
                                            Broadcast(nSender);
                                    }
                                }
                            }
                        });
                    }
                }
                else Broadcast(nSender);
            }
            else
            {
                if (MatchmakingHelper.GetPlayer(networkID, out Player nPlayer))
                {
                    NeutronView nView = nPlayer.NeutronView;
                    if (nView != null)
                    {
                        if (nView.NBs.TryGetValue(instanceID, out NeutronBehaviour neutronBehaviour))
                        {
                            NeutronDispatcher.Dispatch(() =>
                            {
                                using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    nReader.SetBuffer(parameters);
                                    using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                                    {
                                        nWriter.SetLength(0);
                                        {
                                            if (neutronBehaviour.OnNeutronSerializeView(nWriter, nReader, false))
                                                Broadcast(nSender);
                                        }
                                    }
                                }
                            });
                        }
                    }
                    else Broadcast(nPlayer);
                }
            }

            #region Local Functions
            void Broadcast(Player mSender)
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0);
                    nWriter.WritePacket(SystemPacket.SerializeView);
                    nWriter.Write(nSender.ID);
                    nWriter.Write(networkID);
                    nWriter.Write(instanceID);
                    nWriter.WriteExactly(parameters);
                    mSender.Send(nWriter, sendTo, broadcast, recProtocol);
                }
            }
            #endregion
        }
        #endregion
    }
}
using System;
using System.Linq;
using UnityEngine;
using System.IO;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Constants;

namespace NeutronNetwork.Server
{
    public class ServerBase : ServerBehaviour
    {
        #region Singleton
        public static NeutronServer _;
        #endregion

        #region Events
        public static NeutronEventNoReturn m_OnAwake = new NeutronEventNoReturn();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerDisconnected = new NeutronEventNoReturn<NeutronPlayer>();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerDestroyed = new NeutronEventNoReturn<NeutronPlayer>();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerJoinedChannel = new NeutronEventNoReturn<NeutronPlayer>();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerLeaveChannel = new NeutronEventNoReturn<NeutronPlayer>();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerJoinedRoom = new NeutronEventNoReturn<NeutronPlayer>();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerLeaveRoom = new NeutronEventNoReturn<NeutronPlayer>();
        public static NeutronEventNoReturn<NeutronPlayer> m_OnPlayerPropertiesChanged = new NeutronEventNoReturn<NeutronPlayer>();
        #endregion

        #region MonoBehaviour
        public new void Awake()
        {
            base.Awake();
            _ = (NeutronServer)this;
            if (IsReady)
                m_OnAwake.Invoke();
        }
        #endregion

        #region Handles
        protected void DisconnectHandler(NeutronPlayer nPlayer)
        {
            using (nPlayer)
            {
                nPlayer.m_CTS.Cancel();
                if (SocketHelper.RemovePlayerFromServer(nPlayer))
                    m_OnPlayerDisconnected.Invoke(nPlayer);
            }
        }

        protected void HandshakeHandler(NeutronPlayer nSender, double nTime)
        {
            #region Handshake
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.Handshake);
                writer.Write(Neutron.Time);
                writer.Write(nTime);
                writer.Write(nSender.lPEndPoint.Port);
                writer.WriteExactly<NeutronPlayer>(nSender);
                writer.WriteExactly<NeutronPlayer[]>(PlayersBySocket.Values.ToArray()); // send the other players to me.
                nSender.Send(writer);
            }
            #endregion

            #region Players
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.NewPlayer);
                writer.WriteExactly<NeutronPlayer>(nSender); // send me to the other players who were already connected.
                nSender.Send(writer, TargetTo.Others, TunnelingTo.Server, Protocol.Tcp);
            }
            #endregion
        }

        protected void NicknameHandler(NeutronPlayer nSender, string Nickname)
        {
            var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerNicknameChanged;
            nSender.Nickname = Nickname;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.Nickname);
                writer.Write(nSender.ID);
                nSender.Send(writer, handle);
            }
        }

        protected void ChatHandler(NeutronPlayer nSender, ChatPacket chatPacket, TunnelingTo broadcast, int networkID, string message)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.Chat);
                writer.Write(message);
                writer.Write(nSender.ID);

                if (chatPacket == ChatPacket.Global)
                    nSender.Send(writer, TargetTo.All, broadcast, Protocol.Tcp);
                else if (chatPacket == ChatPacket.Private)
                {
                    if (MatchmakingHelper.GetPlayer(networkID, out NeutronPlayer nPlayer))
                    {
                        nPlayer.Send(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Tcp);
                    }
                    else PlayerHelper.Message(nSender, Packet.Chat, "Player not found.");
                }
            }
        }

        protected void iRPCHandler(NeutronPlayer nSender, TunnelingTo broadcast, TargetTo sendTo, Cache cacheMode, int networkID, int attributeID, byte[] parameters, Protocol nRecProtocol)
        {
            #region Logic
            if (nSender.IsInChannel() || nSender.IsInRoom())
            {
                if (SceneHelper.IsSceneObject(networkID))
                {
                    if (MatchmakingHelper.GetNetworkObject(networkID, nSender, out NeutronView nView))
                    {
                        // if (nView.iRPCs.TryGetValue(attributeID, out RPC remoteProceduralCall))
                        // {
                        //     iRPC dynamicAttr = (iRPC)remoteProceduralCall.attribute;
                        //     if (dynamicAttr != null)
                        //     {
                        //         Action _ = () => { };
                        //         #region Object Dispatch Logic
                        //         if (dynamicAttr.SendAfterProcessing)
                        //         {
                        //             _ = new Action(() =>
                        //             {
                        //                 if (DynamicObject(remoteProceduralCall, nView))
                        //                     TunnelingTo(nSender);
                        //                 else return;
                        //             });
                        //         }
                        //         else
                        //         {
                        //             if (TunnelingTo(nSender))
                        //             {
                        //                 _ = new Action(() =>
                        //                 {
                        //                     DynamicObject(remoteProceduralCall, nView);
                        //                 });
                        //             }
                        //         }
                        //         #endregion
                        //         if (dynamicAttr.DispatchOnMainThread)
                        //             NeutronDispatcher.Dispatch(_);
                        //         else _.Invoke();
                        //     }
                        //     else Debug.LogError("Invalid Attribute, there is no valid attribute with this ID.");
                        // }
                        // else Debug.LogError("Invalid gRPC ID, there is no attribute with this ID.");
                    }
                    else Broadcast(nSender);
                }
                else
                {
                    if (MatchmakingHelper.GetPlayer(networkID, out NeutronPlayer nPlayer))
                    {
                        NeutronView neutronView = nPlayer.NeutronView;
                        if (neutronView != null)
                        {
                            // if (neutronView.iRPCs.TryGetValue(attributeID, out RPC remoteProceduralCall))
                            // {
                            //     iRPC dynamicAttr = (iRPC)remoteProceduralCall.attribute;
                            //     if (dynamicAttr != null)
                            //     {
                            //         Action _ = () => { };
                            //         #region Player Dispatch Logic
                            //         if (dynamicAttr.SendAfterProcessing)
                            //         {
                            //             _ = new Action(() =>
                            //             {
                            //                 if (DynamicPlayer(remoteProceduralCall, nPlayer))
                            //                     TunnelingTo(nPlayer);
                            //                 else return;
                            //             });
                            //         }
                            //         else
                            //         {
                            //             if (TunnelingTo(nPlayer))
                            //             {
                            //                 _ = new Action(() =>
                            //                 {
                            //                     DynamicPlayer(remoteProceduralCall, nPlayer);
                            //                 });
                            //             }
                            //         }
                            //         #endregion
                            //         if (dynamicAttr.DispatchOnMainThread)
                            //             NeutronDispatcher.Dispatch(_);
                            //         else _.Invoke();
                            //     }
                            //     else Debug.LogError("Invalid Attribute, there is no valid attribute with this ID.");
                            // }
                            // else Debug.LogError("Invalid iRPC ID, there is no attribute with this ID.");
                        }
                        else Broadcast(nPlayer);
                    }
                    else LogHelper.Error("Invalid Network ID, a player with this ID could not be found.");
                }
            }
            else PlayerHelper.Message(nSender, Packet.iRPC, "ERROR: You are not on a channel/room.");
            #endregion

            #region Local Functions
            bool Broadcast(NeutronPlayer mSender)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(Packet.iRPC);
                    writer.Write(networkID);
                    writer.Write(attributeID);
                    writer.Write(nSender.ID);
                    writer.WriteExactly(parameters);
                    mSender.Send(writer, sendTo, broadcast, nRecProtocol);
                    MatchmakingHelper.SetCache(attributeID, writer.ToArray(), nSender, cacheMode, CachedPacket.iRPC);
                }
                return true;
            }
            bool DynamicPlayer(RPC remoteProceduralCall, NeutronPlayer nPlayer) => PlayerHelper.iRPC(parameters, false, remoteProceduralCall, nSender, nPlayer.NeutronView);
            bool DynamicObject(RPC remoteProceduralCall, NeutronView nView) => PlayerHelper.iRPC(parameters, false, remoteProceduralCall, nSender, nView);
            #endregion
        }

        protected void gRPCHandler(NeutronPlayer nSender, int networkID, int nonDynamicID, byte[] parameters)
        {
            #region Logic
            if (MatchmakingHelper.GetPlayer(networkID, out NeutronPlayer nPlayer))
            {
                if (GlobalBehaviour.gRPCs.TryGetValue((byte)nonDynamicID, out RPC remoteProceduralCall))
                {
                    gRPC nonDynamicAttr = remoteProceduralCall.GRPC;
                    if (nonDynamicAttr != null)
                    {
                        Action _ = () => { };
                        #region Dispatch Logic
                        if (nonDynamicAttr.SendAfterProcessing)
                        {
                            _ = new Action(() =>
                            {
                                if (gRPC(remoteProceduralCall))
                                    Broadcast(nPlayer, nonDynamicAttr.Cache, nonDynamicAttr.TargetTo, nonDynamicAttr.TunnelingTo, nonDynamicAttr.Protocol);
                                else return;
                            });
                        }
                        else
                        {
                            if (Broadcast(nPlayer, nonDynamicAttr.Cache, nonDynamicAttr.TargetTo, nonDynamicAttr.TunnelingTo, nonDynamicAttr.Protocol))
                            {
                                _ = new Action(() =>
                                {
                                    gRPC(remoteProceduralCall);
                                });
                            }
                        }
                        #endregion
                        if (nonDynamicAttr.RunInMonoBehaviour)
                            NeutronDispatcher.Dispatch(_);
                        else _.Invoke();
                    }
                    else Debug.LogError("Invalid Attribute, there is no valid attribute with this ID.");
                }
                else Debug.LogError("Invalid gRPC ID, there is no attribute with this ID.");
            }
            #endregion

            #region Local Functions
            bool Broadcast(NeutronPlayer mSender, Cache cacheMode, TargetTo sendTo, TunnelingTo broadcast, Protocol protocol)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(Packet.gRPC);
                    writer.Write(nonDynamicID);
                    writer.Write(nSender.ID);
                    writer.WriteExactly(parameters);
                    mSender.Send(writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(nonDynamicID, writer.ToArray(), nSender, cacheMode, CachedPacket.gRPC);
                }
                return true;
            }
            bool gRPC(RPC remoteProceduralCall) => PlayerHelper.gRPC(nonDynamicID, nSender, parameters, remoteProceduralCall, true, false);
            #endregion
        }

        protected void GetChannelsHandler(NeutronPlayer nSender)
        {
            try
            {
                if (!nSender.IsInChannel())
                {
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.SetLength(0);
                        NeutronChannel[] channels = ChannelsById.Values.ToArray();
                        writer.WritePacket(Packet.GetChannels);
                        writer.WriteExactly<NeutronChannel[]>(channels);
                        nSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(nSender, Packet.GetChannels, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void JoinChannelHandler(NeutronPlayer nSender, int channelID)
        {
            try
            {
                if (ChannelsById.Count == 0)
                {
                    PlayerHelper.Message(nSender, Packet.JoinChannel, "ERROR: There are no channels created on the server.");
                    return;
                }

                if (ChannelsById.TryGetValue(channelID, out NeutronChannel channel))
                {
                    if (!nSender.IsInChannel())
                    {
                        if (channel.AddPlayer(nSender))
                        {
                            var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerJoinedChannel;
                            nSender.CurrentChannel = channelID;
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.SetLength(0);
                                writer.WritePacket(Packet.JoinChannel);
                                writer.Write(nSender.ID);
                                nSender.Send(writer, handle);
                                Debug.LogError("entrou no canal ):");
                            }
                            nSender.Matchmaking = MatchmakingHelper.Matchmaking(nSender);
                            m_OnPlayerJoinedChannel.Invoke(nSender);
                        }
                        else PlayerHelper.Message(nSender, Packet.JoinChannel, "Failed to add Player");
                    }
                    else PlayerHelper.Message(nSender, Packet.JoinChannel, "ERROR: You are already joined to a channel.");
                }
                else PlayerHelper.Message(nSender, Packet.JoinChannel, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }

        protected void CreateRoomHandler(NeutronPlayer nSender, string roomName, int maxPlayers, string Password, bool isVisible, bool JoinOrCreate, string options)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    NeutronChannel l_Channel = ChannelsById[nSender.CurrentChannel];
                    int ID = l_Channel.CountOfRooms;
                    NeutronRoom nRoom = new NeutronRoom(ID, roomName, maxPlayers, !string.IsNullOrEmpty(Password), isVisible, options);

                    void CreateRoom()
                    {
                        if (l_Channel.AddRoom(nRoom))
                        {
                            nSender.CurrentRoom = ID;
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.SetLength(0);
                                writer.WritePacket(Packet.CreateRoom);
                                writer.WriteExactly<NeutronRoom>(nRoom);
                                nSender.Send(writer);
                            }
                        }
                        else PlayerHelper.Message(nSender, Packet.CreateRoom, "failed create room");
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
                else PlayerHelper.Message(nSender, Packet.CreateRoom, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void GetCacheHandler(NeutronPlayer nSender, CachedPacket packetToSendCache, int ID, bool includeMe)
        {
            INeutronMatchmaking neutronMatchmaking = nSender.Matchmaking;
            if (neutronMatchmaking != null)
            {
                foreach (var l_Cache in neutronMatchmaking.GetCaches())
                {
                    if (!includeMe && l_Cache.Owner.Equals(nSender))
                        continue;
                    if (ID > 0)
                    {
                        if (l_Cache.Packet == packetToSendCache && l_Cache.Id == ID)
                        {
                            if (ID == 1001 || ID == 1002)
                            {
                                #region Pool
                                var lastReader = Neutron.PooledNetworkReaders.Pull();
                                lastReader.SetLength(0);
                                lastReader.SetBuffer(l_Cache.Buffer);
                                lastReader.SetPosition(0);
                                #endregion
                                //using (NeutronReader lastReader = new NeutronReader(l_Cache.buffer))
                                {
                                    Packet packet = lastReader.ReadPacket<Packet>();
                                    int _ID = lastReader.ReadInt32();
                                    byte[] sender = lastReader.ReadExactly();
                                    byte[] parameters = lastReader.ReadExactly();

                                    using (NeutronWriter oldWriter = new NeutronWriter(new MemoryStream(parameters)))
                                    {
                                        NeutronView view = l_Cache.Owner.NeutronView;
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
                            else Broadcast(l_Cache.Buffer);
                        }
                        else continue;
                    }
                    else if (ID == 0)
                    {
                        if (l_Cache.Packet == packetToSendCache)
                            Broadcast(l_Cache.Buffer);
                        else continue;
                    }
                    else if (ID < 0)
                        Broadcast(l_Cache.Buffer);
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

        protected void GetRoomsHandler(NeutronPlayer nSender)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    NeutronChannel channel = ChannelsById[nSender.CurrentChannel];
                    //if (Channels[indexChannel]._rooms.Count == 0) return;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.SetLength(0);
                        NeutronRoom[] rooms = channel.GetRooms();
                        writer.WritePacket(Packet.GetRooms);
                        writer.WriteExactly<NeutronRoom[]>(rooms);
                        nSender.Send(writer);
                    }
                }
                else PlayerHelper.Message(nSender, Packet.GetRooms, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void LeaveRoomHandler(NeutronPlayer nSender)
        {
            if (nSender.IsInRoom())
            {
                var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerLeaveRoom;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(Packet.Leave);
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
            else PlayerHelper.Message(nSender, Packet.Leave, "ERROR: LeaveRoom Failed");
        }

        protected void LeaveChannelHandler(NeutronPlayer nSender)
        {
            if (nSender.IsInChannel())
            {
                var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerLeaveChannel;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.SetLength(0);
                    writer.WritePacket(Packet.Leave);
                    writer.WritePacket(MatchmakingPacket.Channel);
                    writer.Write(nSender.ID);
                    nSender.Send(writer, handle);
                }
                NeutronChannel channel = ChannelsById[nSender.CurrentChannel];
                channel.RemovePlayer(nSender);
                nSender.CurrentChannel = -1;
            }
            else PlayerHelper.Message(nSender, Packet.Leave, "ERROR: LeaveChannel Failed");
        }

        protected void JoinRoomHandler(NeutronPlayer nSender, int roomID)
        {
            try
            {
                if (nSender.IsInChannel() && !nSender.IsInRoom())
                {
                    NeutronChannel channel = ChannelsById[nSender.CurrentChannel]; // Thread safe
                    NeutronRoom room = channel.GetRoom(roomID); // thread safe

                    if (room == null) return;

                    if (room.AddPlayer(nSender))
                    {
                        var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerJoinedRoom;
                        nSender.CurrentRoom = roomID;
                        using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                        {
                            writer.SetLength(0);
                            writer.WritePacket(Packet.JoinRoom);
                            writer.Write(nSender.ID);
                            nSender.Send(writer, handle);
                        }
                        nSender.Matchmaking = MatchmakingHelper.Matchmaking(nSender);
                        m_OnPlayerJoinedRoom.Invoke(nSender);
                    }
                    else PlayerHelper.Message(nSender, Packet.JoinRoom, "Failed to add player.");
                }
                else PlayerHelper.Message(nSender, Packet.JoinRoom, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void DestroyPlayerHandler(NeutronPlayer nSender)
        {
            var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerDestroyed;
            //MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.DestroyPlayer);
                nSender.Send(writer, handle);
            }
            m_OnPlayerDestroyed.Invoke(nSender);
        }

        public void SetPlayerPropertiesHandler(NeutronPlayer nSender, string properties)
        {
            var handle = NeutronMain.Synchronization.DefaultHandlers.OnPlayerPropertiesChanged;
            nSender._ = properties;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.Write(nSender.ID);
                nSender.Send(writer, handle);
            }
        }

        public void SetRoomPropertiesHandler(NeutronPlayer nSender, string properties)
        {
            if (nSender.IsInRoom())
            {
                NeutronChannel channel = ChannelsById[nSender.CurrentChannel];
                NeutronRoom room = channel.GetRoom(nSender.CurrentRoom);

                if (room.Owner == null)
                {
                    PlayerHelper.Message(nSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
                    return;
                }

                if (room.Owner.Equals(nSender))
                {
                    var handle = NeutronMain.Synchronization.DefaultHandlers.OnRoomPropertiesChanged;
                    room._ = properties;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.SetLength(0);
                        writer.WritePacket(Packet.SetRoomProperties);
                        writer.Write(nSender.ID);
                        nSender.Send(writer, handle);
                    }
                }
                else PlayerHelper.Message(nSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else PlayerHelper.Message(nSender, Packet.SetRoomProperties, "You are not inside a room.");
        }

        public void PingHandler(NeutronPlayer nSender, double nTime)
        {
            double nDiffTime = Math.Abs(Neutron.Time - nTime);
            if ((Neutron.Time > nTime) && nDiffTime > NeutronConstants.NETWORK_TIME_DESYNCHRONIZATION_TOLERANCE)
                Debug.LogError($"Jogador {nSender.Nickname} atraso em {nDiffTime} Ms!");
            else if ((nTime > Neutron.Time) && nDiffTime > NeutronConstants.NETWORK_TIME_DESYNCHRONIZATION_TOLERANCE)
                Debug.LogError($"Jogador {nSender.Nickname} adiantado em {nDiffTime} Ms!");
            using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
            {
                nWriter.SetLength(0);
                nWriter.WritePacket(Packet.Ping);
                nWriter.Write(Neutron.Time);
                nWriter.Write(nTime);
                nSender.Send(nWriter, TargetTo.Me, TunnelingTo.Me, Protocol.Udp);
            }
        }

        public void ClientPacketHandler(NeutronPlayer nSender, bool isMine, int networkID, byte[] parameters, CustomPacket clientPacket, TargetTo sendTo, TunnelingTo broadcast, Protocol recProtocol)
        {
            if (MatchmakingHelper.GetPlayer(networkID, out NeutronPlayer nPlayer))
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0);
                    nWriter.WritePacket(Packet.CustomPacket);
                    nWriter.WritePacket(clientPacket);
                    nWriter.Write(nSender.ID);
                    nWriter.WriteExactly(parameters);

                    if (isMine)
                        nPlayer.Send(nWriter, sendTo, broadcast, recProtocol);
                    else nPlayer.Send(nWriter, TargetTo.Me, TunnelingTo.Me, recProtocol);
                }
            }
            else Debug.LogError("dsdsdsd");
        }

        public void OnSerializeViewHandler(NeutronPlayer nSender, int networkID, int instanceID, byte[] parameters, TargetTo sendTo, TunnelingTo broadcast, Protocol recProtocol)
        {
#if UNITY_EDITOR
            ThreadHelper.DoNotAllowSimultaneousAccess(ServerDataProcessingStackManagedThreadId);
#endif
            if (nSender.IsInChannel() || nSender.IsInRoom())
            {
                if (SceneHelper.IsSceneObject(networkID))
                {
                    if (MatchmakingHelper.GetNetworkObject(networkID, nSender, out NeutronView nView))
                    {
                        if (nView.neutronBehaviours.TryGetValue(instanceID, out NeutronBehaviour neutronBehaviour))
                        {
                            //nView.Dispatch(() =>
                            //{
                            using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                            {
                                nReader.SetBuffer(parameters);
                                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                                {
                                    nWriter.SetLength(0);
                                    if (neutronBehaviour.OnAutoSynchronization(nWriter, nReader, false))
                                        Broadcast(nSender);
                                }
                            }
                            //});
                        }
                    }
                    else Broadcast(nSender);
                }
                else
                {
                    if (MatchmakingHelper.GetPlayer(networkID, out NeutronPlayer nPlayer))
                    {
                        NeutronView nView = nPlayer.NeutronView;
                        if (nView != null)
                        {
                            if (nView.neutronBehaviours.TryGetValue(instanceID, out NeutronBehaviour neutronBehaviour))
                            {
                                //NeutronDispatcher.Dispatch(() =>
                                {
                                    using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                                    {
                                        nReader.SetBuffer(parameters);
                                        using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                                        {
                                            nWriter.SetLength(0);
                                            if (neutronBehaviour.OnAutoSynchronization(nWriter, nReader, false))
                                                Broadcast(nSender); //! error
                                        }
                                    }
                                }//);
                            }
                        }
                        else Broadcast(nPlayer);
                    }
                }
            }

            #region Local Functions
            void Broadcast(NeutronPlayer mSender)
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0);
                    nWriter.WritePacket(Packet.OnAutoSync);
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
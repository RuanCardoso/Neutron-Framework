using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Server
{
    public class ServerBase : ServerBehaviour
    {
        #region Singleton
        public static NeutronServer This;
        #endregion

        #region Events
        public static NeutronEventNoReturn OnAwake { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerDisconnected { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerDestroyed { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerJoinedChannel { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerLeaveChannel { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerJoinedRoom { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerLeaveRoom { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerPropertiesChanged { get; set; }
        #endregion

        #region MonoBehaviour
        public new void Awake()
        {
            base.Awake();
            This = (NeutronServer)this;
            if (IsReady)
                OnAwake?.Invoke();
        }
        #endregion

        #region Handles
        protected void DisconnectHandler(NeutronPlayer player)
        {
            using (player)
            {
                player.TokenSource.Cancel();
                if (SocketHelper.RemovePlayerFromServer(player))
                    OnPlayerDisconnected?.Invoke(player);
            }
        }

        protected void HandshakeHandler(NeutronPlayer player, double time)
        {
            #region Handshake
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Handshake);
                writer.Write(Neutron.Time);
                writer.Write(time);
                writer.Write(player.LocalEndPoint.Port);
                writer.WriteExactly(player);
                writer.WriteExactly(PlayersBySocket.Values.ToArray()); // send the other players to me.
                player.Send(writer);
            }
            #endregion

            #region Players
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.NewPlayer);
                writer.WriteExactly(player); // send me to the other players who were already connected.
                player.Send(writer, OthersHelper.GetDefaultHandler().OnGetAllPlayersOnConnection);
            }
            #endregion
        }

        protected void NicknameHandler(NeutronPlayer player, string nickname)
        {
            player.Nickname = nickname;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Nickname);
                writer.Write(player.ID);
                writer.Write(nickname);
                player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerNicknameChanged);
            }
        }

        protected void ChatHandler(NeutronPlayer player, ChatPacket packet, TunnelingTo tunnelingTo, int viewId, string message)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Chat);
                writer.Write(message);
                writer.Write(player.ID);

                if (packet == ChatPacket.Global)
                    player.Send(writer, TargetTo.All, tunnelingTo, Protocol.Tcp);
                else if (packet == ChatPacket.Private)
                {
                    if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer playerFound))
                    {
                        playerFound.Send(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Tcp);
                    }
                    else
                        PlayerHelper.Message(player, Packet.Chat, "Player not found.");
                }
            }
        }

#pragma warning disable IDE1006
        protected void iRPCHandler(NeutronPlayer player, TunnelingTo tunnelingTo, TargetTo targetTo, Cache cache, int viewId, int id, byte[] buffer, Protocol protocol)
#pragma warning restore IDE1006
        {
            #region Logic
            if (player.IsInChannel() || player.IsInRoom())
            {
                if (SceneHelper.IsSceneObject(viewId))
                {
                    if (MatchmakingHelper.GetNetworkObject((player.ID, viewId), player, out NeutronView nView))
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
                    else Broadcast(player);
                }
                else
                {
                    if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer nPlayer))
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
            else PlayerHelper.Message(player, Packet.iRPC, "ERROR: You are not on a channel/room.");
            #endregion

            #region Local Functions
            bool Broadcast(NeutronPlayer mSender)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket(Packet.iRPC);
                    writer.Write(viewId);
                    writer.Write(id);
                    writer.Write(player.ID);
                    writer.WriteExactly(buffer);
                    mSender.Send(writer, targetTo, tunnelingTo, protocol);
                    MatchmakingHelper.SetCache(id, writer.ToArray(), player, cache, CachedPacket.iRPC);
                }
                return true;
            }
            bool DynamicPlayer(RPC remoteProceduralCall, NeutronPlayer nPlayer) => PlayerHelper.iRPC(buffer, false, remoteProceduralCall, player);
            bool DynamicObject(RPC remoteProceduralCall, NeutronView nView) => PlayerHelper.iRPC(buffer, false, remoteProceduralCall, player);
            #endregion
        }

#pragma warning disable IDE1006
        protected async void gRPCHandler(NeutronPlayer player, int viewId, int id, byte[] buffer)
#pragma warning restore IDE1006
        {
            #region Logic
            if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer nPlayer))
            {
                if (GlobalBehaviour.gRPCs.TryGetValue((byte)id, out RPC remoteProceduralCall))
                {
                    gRPC nonDynamicAttr = remoteProceduralCall.GRPC;
                    if (nonDynamicAttr != null)
                    {
                        Action _ = () => { };
                        #region Dispatch Logic
                        //if (nonDynamicAttr.SendAfterProcessing)
                        //{
                        //    _ = new Action(async () =>
                        //    {
                        //        if (await gRPC(remoteProceduralCall))
                        //            Broadcast(nPlayer, nonDynamicAttr.Cache, nonDynamicAttr.TargetTo, nonDynamicAttr.TunnelingTo, nonDynamicAttr.Protocol);
                        //        else return;
                        //    });
                        //}
                        //else
                        //{
                        //    if (Broadcast(nPlayer, nonDynamicAttr.Cache, nonDynamicAttr.TargetTo, nonDynamicAttr.TunnelingTo, nonDynamicAttr.Protocol))
                        //    {
                        //        _ = new Action(async () =>
                        //        {
                        //            await gRPC(remoteProceduralCall);
                        //        });
                        //    }
                        //}
                        #endregion
                        //if (nonDynamicAttr.RunInMonoBehaviour) { }
                        //NeutronDispatcher.Dispatch(_);
                        //else
                        {
                            try
                            {
                                if (await gRPC(remoteProceduralCall))
                                    Broadcast(nPlayer, nonDynamicAttr.Cache, nonDynamicAttr.TargetTo, nonDynamicAttr.TunnelingTo, nonDynamicAttr.Protocol);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.StackTrace(ex);
                            }
                        }
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
                    writer.WritePacket(Packet.gRPC);
                    writer.Write(id);
                    writer.Write(player.ID);
                    writer.WriteExactly(buffer);
                    mSender.Send(writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(id, writer.ToArray(), player, cacheMode, CachedPacket.gRPC);
                }
                return true;
            }
            async Task<bool> gRPC(RPC remoteProceduralCall) => await PlayerHelper.gRPC(id, player, buffer, remoteProceduralCall, true, false);
            #endregion
        }

        protected void GetChannelsHandler(NeutronPlayer player)
        {
            try
            {
                if (!player.IsInChannel())
                {
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        NeutronChannel[] channels = ChannelsById.Values.ToArray();
                        writer.WritePacket(Packet.GetChannels);
                        writer.WriteExactly(channels);
                        player.Send(writer);
                    }
                }
                else PlayerHelper.Message(player, Packet.GetChannels, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void JoinChannelHandler(NeutronPlayer player, int channelId)
        {
            try
            {
                if (ChannelsById.Count == 0)
                {
                    PlayerHelper.Message(player, Packet.JoinChannel, "ERROR: There are no channels created on the server.");
                    return;
                }

                if (ChannelsById.TryGetValue(channelId, out NeutronChannel channel))
                {
                    if (!player.IsInChannel())
                    {
                        if (channel.Add(player))
                        {
                            player.Channel = channel;
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.WritePacket(Packet.JoinChannel);
                                writer.Write(player.ID);
                                writer.WriteExactly(channel);
                                player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerJoinedChannel);
                            }
                            player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                            OnPlayerJoinedChannel?.Invoke(player);
                        }
                        else PlayerHelper.Message(player, Packet.JoinChannel, "Failed to add Player");
                    }
                    else PlayerHelper.Message(player, Packet.JoinChannel, "ERROR: You are already joined to a channel.");
                }
                else PlayerHelper.Message(player, Packet.JoinChannel, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }

        protected void CreateRoomHandler(NeutronPlayer player, string roomName, int maxPlayers, string password, bool isVisible, bool joinOrCreate, string options)
        {
            try
            {
                if (player.IsInChannel() && !player.IsInRoom())
                {
                    NeutronChannel l_Channel = player.Channel;
                    int ID = l_Channel.RoomCount;
                    NeutronRoom nRoom = new NeutronRoom(ID, roomName, maxPlayers, !string.IsNullOrEmpty(password), isVisible, options);

                    void CreateRoom()
                    {
                        if (l_Channel.AddRoom(nRoom))
                        {
                            player.Room = nRoom;
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.WritePacket(Packet.CreateRoom);
                                writer.Write(player.ID);
                                writer.WriteExactly(nRoom);
                                player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerCreatedRoom);
                            }
                        }
                        else PlayerHelper.Message(player, Packet.CreateRoom, "failed create room");
                    }

                    if (!joinOrCreate)
                    {
                        CreateRoom();
                    }
                    else
                    {
                        if (!player.Channel.GetRoom(roomName))
                        {
                            CreateRoom();
                        }
                        else JoinRoomHandler(player, ID);
                    }
                }
                else PlayerHelper.Message(player, Packet.CreateRoom, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void GetCacheHandler(NeutronPlayer player, CachedPacket cachedPacket, int id, bool sendToOwner)
        {
            INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
            if (neutronMatchmaking != null)
            {
                foreach (var l_Cache in neutronMatchmaking.Caches())
                {
                    if (!sendToOwner && l_Cache.Owner.Equals(player))
                        continue;
                    if (id > 0)
                    {
                        if (l_Cache.Packet == cachedPacket && l_Cache.Id == id)
                        {
                            if (id == 1001 || id == 1002)
                            {
                                #region Pool
                                var lastReader = Neutron.PooledNetworkReaders.Pull();
                                lastReader.SetBuffer(l_Cache.Buffer);
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
                                                writer.WritePacket(packet);
                                                writer.Write(_ID);
                                                writer.WriteExactly(sender);
                                                writer.WriteExactly(parameters);
                                                player.Send(writer);
                                            }
                                        }
                                    }
                                }
                            }
                            else Broadcast(l_Cache.Buffer);
                        }
                        else continue;
                    }
                    else if (id == 0)
                    {
                        if (l_Cache.Packet == cachedPacket)
                            Broadcast(l_Cache.Buffer);
                        else continue;
                    }
                    else if (id < 0)
                        Broadcast(l_Cache.Buffer);
                }
            }

            void Broadcast(byte[] buffer)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.Write(buffer);
                    player.Send(writer);
                }
            }
        }

        protected void GetRoomsHandler(NeutronPlayer player)
        {
            try
            {
                if (player.IsInChannel() && !player.IsInRoom())
                {
                    NeutronChannel channel = player.Channel;
                    if (channel.RoomCount <= 0)
                    {
                        PlayerHelper.Message(player, Packet.GetRooms, "sem salas pau no cú");
                        return;
                    }
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        NeutronRoom[] rooms = channel.GetRooms();
                        writer.WritePacket(Packet.GetRooms);
                        writer.WriteExactly<NeutronRoom[]>(rooms);
                        player.Send(writer);
                    }
                }
                else PlayerHelper.Message(player, Packet.GetRooms, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void LeaveRoomHandler(NeutronPlayer player)
        {
            if (player.IsInRoom())
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket(Packet.Leave);
                    writer.WritePacket(MatchmakingPacket.Room);
                    writer.Write(player.ID);
                    writer.WriteExactly(player.Room);
                    player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerLeaveRoom);
                }

                INeutronMatchmaking matchmaking = player.Matchmaking;
                if (matchmaking != null)
                {
                    if (matchmaking.Remove(player))
                        MatchmakingHelper.Leave(player, leaveChannel: false);
                }
            }
            else PlayerHelper.Message(player, Packet.Leave, "ERROR: LeaveRoom Failed");
        }

        protected void LeaveChannelHandler(NeutronPlayer player)
        {
            if (player.IsInChannel())
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket(Packet.Leave);
                    writer.WritePacket(MatchmakingPacket.Channel);
                    writer.Write(player.ID);
                    writer.WriteExactly(player.Channel);
                    player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerLeaveChannel);
                }

                NeutronChannel channel = player.Channel;
                channel.Remove(player);
                player.Channel = null;
            }
            else PlayerHelper.Message(player, Packet.Leave, "ERROR: LeaveChannel Failed");
        }

        protected void JoinRoomHandler(NeutronPlayer player, int roomId)
        {
            try
            {
                if (player.IsInChannel() && !player.IsInRoom())
                {
                    NeutronChannel channel = player.Channel; // Thread safe
                    NeutronRoom room = channel.GetRoom(roomId); // thread safe

                    if (room == null) return;

                    if (room.Add(player))
                    {
                        player.Room = room;
                        using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                        {
                            writer.WritePacket(Packet.JoinRoom);
                            writer.Write(player.ID);
                            writer.WriteExactly(room);
                            player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerJoinedRoom);
                        }
                        player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                        OnPlayerJoinedRoom?.Invoke(player);
                    }
                    else PlayerHelper.Message(player, Packet.JoinRoom, "Failed to add player.");
                }
                else PlayerHelper.Message(player, Packet.JoinRoom, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        protected void DestroyPlayerHandler(NeutronPlayer player)
        {
            //MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.DestroyPlayer);
                player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerDestroyed);
            }
            OnPlayerDestroyed?.Invoke(player);
        }

        public void SetPlayerPropertiesHandler(NeutronPlayer player, string properties)
        {
            player.Properties = properties;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.Write(player.ID);
                player.Send(writer, OthersHelper.GetDefaultHandler().OnPlayerPropertiesChanged);
            }
        }

        public void SetRoomPropertiesHandler(NeutronPlayer player, string properties)
        {
            if (player.IsInRoom())
            {
                NeutronChannel channel = player.Channel;
                NeutronRoom room = player.Room;

                if (room.Owner == null)
                {
                    PlayerHelper.Message(player, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
                    return;
                }

                if (room.Owner.Equals(player))
                {
                    room.Properties = properties;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket(Packet.SetRoomProperties);
                        writer.Write(player.ID);
                        player.Send(writer, OthersHelper.GetDefaultHandler().OnRoomPropertiesChanged);
                    }
                }
                else PlayerHelper.Message(player, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else PlayerHelper.Message(player, Packet.SetRoomProperties, "You are not inside a room.");
        }

        public void PingHandler(NeutronPlayer player, double time)
        {
            double diffTime = Math.Abs(Neutron.Time - time);
            if ((Neutron.Time > time) && diffTime > OthersHelper.GetSettings().NET_TIME_DESYNC_TOLERANCE)
                Debug.LogError($"Jogador {player.Nickname} atraso em {diffTime} Ms!");
            else if ((time > Neutron.Time) && diffTime > OthersHelper.GetSettings().NET_TIME_DESYNC_TOLERANCE)
                Debug.LogError($"Jogador {player.Nickname} adiantado em {diffTime} Ms!");
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Ping);
                writer.Write(Neutron.Time);
                writer.Write(time);
                player.Send(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Udp);
            }
        }

        public void CustomPacketHandler(NeutronPlayer player, bool isMine, int viewId, byte[] parameters, CustomPacket packet, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer nPlayer))
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket(Packet.CustomPacket);
                    writer.WritePacket(packet);
                    writer.Write(player.ID);
                    writer.WriteExactly(parameters);

                    if (isMine)
                        nPlayer.Send(writer, targetTo, tunnelingTo, protocol);
                    else
                        nPlayer.Send(writer, TargetTo.Me, TunnelingTo.Me, protocol);
                }
            }
            else Debug.LogError("dsdsdsd");
        }

        public void OnAutoSyncHandler(NeutronPlayer player, int viewId, int instanceId, byte[] buffer, RegisterType registerType, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
#if UNITY_EDITOR
            ThreadHelper.DoNotAllowSimultaneousAccess(PacketProcessingStack_ManagedThreadId);
#endif
            void Run((int, int) key)
            {
                void Send()
                {
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket(Packet.OnAutoSync);
                        writer.WritePacket(registerType);
                        writer.Write(player.ID);
                        writer.Write(viewId);
                        writer.Write(instanceId);
                        writer.WriteExactly(buffer);
                        player.Send(writer, targetTo, tunnelingTo, protocol);
                    }
                }

                if (MatchmakingHelper.GetNetworkObject(key, player, out NeutronView neutronView))
                {
                    if (neutronView.Childs.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                    {
                        NeutronReader reader = Neutron.PooledNetworkReaders.Pull();
                        NeutronWriter writer = Neutron.PooledNetworkWriters.Pull();
                        reader.SetBuffer(buffer);
                        if (neutronBehaviour.OnAutoSynchronization(writer, reader, false))
                            Send();
                    }
                    else
                        PlayerHelper.Message(player, Packet.OnAutoSync, "Auto Sync instance not found!");
                }
                else
                    Send();
            }

            if (player.IsInMatchmaking())
            {
                switch (registerType)
                {
                    case RegisterType.Scene:
                        Run((0, viewId));
                        break;
                    case RegisterType.Player:
                        Run((viewId, viewId));
                        break;
                    case RegisterType.Dynamic:
                        Run((player.ID, viewId));
                        break;
                }
            }
            else
                PlayerHelper.Message(player, Packet.OnAutoSync, "Have you ever joined a channel or room?");
        }
        #endregion
    }
}
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

        #region Handlers
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
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Handshake);
                writer.Write(Neutron.Time);
                writer.Write(time);
                writer.Write(player.LocalEndPoint.Port);
                writer.WriteExactly(player);
                writer.WriteExactly(PlayersBySocket.Values.ToArray()); // send the other players to me.
                player.Write(writer);
            }

            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.NewPlayer);
                writer.WriteExactly(player); // send me to the other players who were already connected.
                player.Write(writer, TargetTo.Others, TunnelingTo.Server, Protocol.Tcp);
            }
        }

        protected void SetNicknameHandler(NeutronPlayer player, string nickname)
        {
            player.Nickname = nickname;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Nickname);
                writer.Write(player.ID);
                writer.Write(nickname);
                player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerNicknameChanged);
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
                    player.Write(writer, TargetTo.All, tunnelingTo, Protocol.Tcp);
                else if (packet == ChatPacket.Private)
                {
                    if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer playerFound))
                        playerFound.Write(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Tcp);
                    else
                        player.Message(Packet.Chat, "Player not found!");
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
            else player.Message(Packet.iRPC, "ERROR: You are not on a channel/room.");
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
                    mSender.Write(writer, targetTo, tunnelingTo, protocol);
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
                    writer.WriteExactly(buffer);
                    mSender.Write(player, writer, sendTo, broadcast, protocol);
                    MatchmakingHelper.SetCache(id, writer.ToArray(), player, cacheMode, CachedPacket.gRPC);
                }
                return true;
            }
            async Task<bool> gRPC(RPC remoteProceduralCall) => await PlayerHelper.gRPC(id, player, buffer, remoteProceduralCall, true, false);
            #endregion
        }

        protected void GetChannelsHandler(NeutronPlayer player)
        {
            if (!player.IsInMatchmaking())
            {
                NeutronChannel[] channels = ChannelsById.Values.ToArray();
                if (channels.Length > 0)
                {
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket(Packet.GetChannels);
                        writer.WriteExactly(channels);
                        player.Write(writer);
                    }
                }
                else
                    player.Message(Packet.GetChannels, "No channels found!");
            }
            else
                player.Message(Packet.GetChannels, "You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
        }

        protected void GetRoomsHandler(NeutronPlayer player)
        {
            if (player.IsInChannel() && !player.IsInRoom())
            {
                NeutronChannel channel = player.Channel;
                if (channel.RoomCount > 0)
                {
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        NeutronRoom[] rooms = channel.GetRooms(x => x.IsVisible);
                        writer.WritePacket(Packet.GetRooms);
                        writer.WriteExactly(rooms);
                        player.Write(writer);
                    }
                }
                else
                    player.Message(Packet.GetRooms, "No rooms found!");
            }
            else
                player.Message(Packet.GetRooms, "You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
        }

        protected void JoinChannelHandler(NeutronPlayer player, int channelId)
        {
            if (ChannelsById.Count > 0)
            {
                if (ChannelsById.TryGetValue(channelId, out NeutronChannel channel))
                {
                    if (!player.IsInChannel())
                    {
                        if (channel.Add(player))
                        {
                            player.Channel = channel;
                            player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                            OnPlayerJoinedChannel?.Invoke(player);
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.WritePacket(Packet.JoinChannel);
                                writer.Write(player.ID);
                                writer.WriteExactly(channel);
                                player.Write(writer, TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
                            }
                        }
                        else
                            player.Message(Packet.JoinChannel, "Failed to join channel");
                    }
                    else
                        player.Message(Packet.JoinChannel, "You are already joined to a channel.");
                }
                else
                    player.Message(Packet.JoinChannel, "We couldn't find a channel with this ID.");
            }
            else
                player.Message(Packet.JoinChannel, "There are no channels created on the server.");
        }

        protected void JoinRoomHandler(NeutronPlayer player, int roomId)
        {
            if (player.IsInChannel() && !player.IsInRoom())
            {
                NeutronRoom room = player.Channel.GetRoom(roomId);

                if (room.Add(player))
                {
                    player.Room = room;
                    player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                    OnPlayerJoinedRoom?.Invoke(player);
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket(Packet.JoinRoom);
                        writer.Write(player.ID);
                        writer.WriteExactly(room);
                        player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerJoinedRoom);
                    }
                }
                else
                    player.Message(Packet.JoinRoom, "Failed to add player.");
            }
            else
                player.Message(Packet.JoinRoom, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
        }

        protected void CreateRoomHandler(NeutronPlayer player, NeutronRoom room, string password)
        {
            if (player.IsInChannel() && !player.IsInRoom())
            {
                NeutronChannel channel = player.Channel;

                int id = channel.RoomCount;
                room.ID = ++id;
                room.Password = password;
                room.Player = player;

                if (!string.IsNullOrEmpty(room.Name))
                {
                    if (channel.AddRoom(room))
                    {
                        using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                        {
                            writer.WritePacket(Packet.CreateRoom);
                            writer.Write(player.ID);
                            writer.WriteExactly(room);
                            player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerCreatedRoom);
                        }
                        JoinRoomHandler(player, room.ID);
                    }
                    else
                        player.Message(Packet.CreateRoom, "Failed to create room!");
                }
                else
                    player.Message(Packet.CreateRoom, "Room name is null or empty!");
            }
            else
                player.Message(Packet.CreateRoom, "You cannot create a room by being inside one.\r\nCall LeaveRoom or you not within a channel!");
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
                                                player.Write(writer);
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
                    player.Write(writer);
                }
            }
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
                    player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerLeaveRoom);
                }

                INeutronMatchmaking matchmaking = player.Matchmaking;
                if (matchmaking != null)
                {
                    if (matchmaking.Remove(player))
                        MatchmakingHelper.Leave(player, leaveChannel: false);
                }
            }
            else player.Message(Packet.Leave, "ERROR: LeaveRoom Failed");
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
                    player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerLeaveChannel);
                }

                NeutronChannel channel = player.Channel;
                channel.Remove(player);
                player.Channel = null;
            }
            else player.Message(Packet.Leave, "ERROR: LeaveChannel Failed");
        }

        protected void DestroyPlayerHandler(NeutronPlayer player)
        {
            //MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.DestroyPlayer);
                player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerDestroyed);
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
                player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerPropertiesChanged);
            }
        }

        public void SetRoomPropertiesHandler(NeutronPlayer player, string properties)
        {
            if (player.IsInRoom())
            {
                NeutronChannel channel = player.Channel;
                NeutronRoom room = player.Room;

                if (room.Player == null)
                {
                    player.Message(Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
                    return;
                }

                if (room.Player.Equals(player))
                {
                    room.Properties = properties;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket(Packet.SetRoomProperties);
                        writer.Write(player.ID);
                        player.Write(writer, OthersHelper.GetDefaultHandler().OnRoomPropertiesChanged);
                    }
                }
                else player.Message(Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else player.Message(Packet.SetRoomProperties, "You are not inside a room.");
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
                player.Write(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Udp, Packet.Ping);
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
                        nPlayer.Write(writer, targetTo, tunnelingTo, protocol);
                    else
                        nPlayer.Write(writer, TargetTo.Me, TunnelingTo.Me, protocol);
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
                        player.Write(writer, targetTo, tunnelingTo, protocol);
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
                        player.Message(Packet.OnAutoSync, "Auto Sync instance not found!");
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
                player.Message(Packet.OnAutoSync, "Have you ever joined a channel or room?");
        }
        #endregion
    }
}
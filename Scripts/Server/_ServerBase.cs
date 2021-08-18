using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
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
        public static NeutronServer This { get; set; }
        #endregion

        #region Events
        public static NeutronEventNoReturn OnAwake { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerDisconnected { get; set; }
        public static NeutronEventWithReturn<NeutronPlayer, string, bool> OnPlayerNicknameChanged { get; set; }
        public static NeutronEventWithReturn<NeutronPlayer, string, bool> OnMessageReceived { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerDestroyed { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerJoinedChannel { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerJoinedRoom { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer, NeutronRoom> OnPlayerCreatedRoom { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerLeftChannel { get; set; }
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerLeftRoom { get; set; }
        public static NeutronEventWithReturn<NeutronPlayer, string, bool> OnPlayerPropertiesChanged { get; set; }
        public static NeutronEventWithReturn<NeutronPlayer, string, bool> OnRoomPropertiesChanged { get; set; }
        public static NeutronEventWithReturn<NeutronPlayer, TunnelingTo, NeutronPlayer[]> OnCustomTunneling { get; set; }
        #endregion

        #region Mono Behaviour
        protected override void Awake()
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
                writer.WritePacket((byte)Packet.Handshake);
                writer.Write(Neutron.Time);
                writer.Write(time);
                writer.Write(player.StateObject.UdpLocalEndPoint.Port);
                writer.WriteIntExactly(player);
                writer.WriteIntExactly(PlayersBySocket.Values.ToArray()); // send the other players to me.
                player.Write(writer);
            }

            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.NewPlayer);
                writer.WriteIntExactly(player); // send me to the other players who were already connected.
                player.Write(writer, TargetTo.Others, TunnelingTo.Server, Protocol.Tcp);
            }
        }

        protected void SetNicknameHandler(NeutronPlayer player, string nickname)
        {
            if (OnPlayerNicknameChanged.Invoke(player, nickname))
            {
                player.Nickname = nickname;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.Nickname);
                    writer.Write(player.ID);
                    writer.Write(nickname);
                    player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerNicknameChanged);
                }
            }
        }

        protected void ChatHandler(NeutronPlayer player, ChatMode packet, TunnelingTo tunnelingTo, int viewId, string message)
        {
            if (OnMessageReceived.Invoke(player, message))
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.Chat);
                    writer.Write(message);
                    writer.Write(player.ID);

                    if (packet == ChatMode.Global)
                        player.Write(writer, TargetTo.All, tunnelingTo, Protocol.Tcp);
                    else if (packet == ChatMode.Private)
                    {
                        if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer playerFound))
                            playerFound.Write(player, writer, TargetTo.Me, TunnelingTo.Me, Protocol.Tcp);
                        else
                            player.Message(Packet.Chat, "Player not found!");
                    }
                }
            }
        }

#pragma warning disable IDE1006
        protected async void iRPCHandler(NeutronPlayer owner, NeutronPlayer sender, short viewId, byte rpcId, byte instanceId, byte[] buffer, RegisterMode registerType, TargetTo targetTo, CacheMode cache, Protocol protocol)
#pragma warning restore IDE1006
        {
            async Task Run((int, int, RegisterMode) key)
            {
                bool Send()
                {
                    TunnelingTo tunnelingTo = TunnelingTo.Auto;
                    if (targetTo == TargetTo.Me)
                        tunnelingTo = TunnelingTo.Me;
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket((byte)Packet.iRPC);
                        writer.WritePacket((byte)registerType);
                        writer.Write(viewId);
                        writer.Write(rpcId);
                        writer.Write(instanceId);
                        writer.WriteNextBytes(buffer);
                        //////////////////////////////////////////////////////////////////////////////////
                        MatchmakingHelper.AddCache(rpcId, viewId, writer, owner, cache, CachedPacket.iRPC);
                        //////////////////////////////////////////////////////////////////////////////////
                        owner.Write(sender, writer, targetTo, tunnelingTo, protocol);
                    }
                    return true;
                }

                if (MatchmakingHelper.GetNetworkObject(key, owner, out NeutronView neutronView))
                {
                    if (neutronView.iRPCs.TryGetValue((rpcId, instanceId), out RPCInvoker remoteProceduralCall))
                    {
                        try
                        {
                            iRPC iRPCAttribute = remoteProceduralCall.iRPC;
                            if (iRPCAttribute.FirstValidation)
                            {
                                if (await ReflectionHelper.iRPC(buffer, remoteProceduralCall, owner))
                                    Send();
                            }
                            else
                            {
                                if (Send())
                                    await ReflectionHelper.iRPC(buffer, remoteProceduralCall, owner);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.StackTrace(ex);
                        }
                    }
                    else
                        Send();
                }
                else
                    Send();
            }

            if (owner.IsInMatchmaking())
            {
                switch (registerType)
                {
                    case RegisterMode.Scene:
                        await Run((0, viewId, registerType));
                        break;
                    case RegisterMode.Player:
                        await Run((viewId, viewId, registerType));
                        break;
                    case RegisterMode.Dynamic:
                        await Run((owner.ID, viewId, registerType));
                        break;
                }
            }
            else
                owner.Message(Packet.iRPC, "Have you ever joined a channel or room?");
        }

#pragma warning disable IDE1006
        protected async void gRPCHandler(NeutronPlayer owner, NeutronPlayer sender, byte id, byte[] buffer, Protocol protocol)
#pragma warning restore IDE1006
        {
            bool Send(CacheMode cache, TargetTo targetTo, TunnelingTo tunnelingTo)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.gRPC);
                    writer.Write(id);
                    writer.WriteNextBytes(buffer);
                    //////////////////////////////////////////////////////////////////////////////////
                    MatchmakingHelper.AddCache(id, 0, writer, owner, cache, CachedPacket.gRPC);
                    //////////////////////////////////////////////////////////////////////////////////
                    owner.Write(sender, writer, targetTo, tunnelingTo, protocol);
                }
                return true;
            }

            if (GlobalBehaviour.gRPCs.TryGetValue(id, out RPCInvoker remoteProceduralCall))
            {
                try
                {
                    gRPC gRPCAttribute = remoteProceduralCall.gRPC;
                    if (gRPCAttribute.FirstValidation)
                    {
                        if (await ReflectionHelper.gRPC(owner, buffer, remoteProceduralCall, true, false, NeutronServer.Neutron))
                            Send(gRPCAttribute.Cache, gRPCAttribute.TargetTo, gRPCAttribute.TunnelingTo);
                    }
                    else
                    {
                        if (Send(gRPCAttribute.Cache, gRPCAttribute.TargetTo, gRPCAttribute.TunnelingTo))
                            await ReflectionHelper.gRPC(owner, buffer, remoteProceduralCall, true, false, NeutronServer.Neutron);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.StackTrace(ex);
                }
            }
            else
                owner.Message(Packet.gRPC, "Invalid gRPC ID, there is no attribute with this ID.");
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
                        writer.WritePacket((byte)Packet.GetChannels);
                        writer.WriteIntExactly(channels);
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
                        writer.WritePacket((byte)Packet.GetRooms);
                        writer.WriteIntExactly(rooms);
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
                            //*****************************************************************
                            OnPlayerJoinedChannel?.Invoke(player);
                            //*****************************************************************
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.WritePacket((byte)Packet.JoinChannel);
                                writer.Write(player.ID);
                                writer.WriteIntExactly(channel);
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
                    //*****************************************************************
                    OnPlayerJoinedRoom?.Invoke(player);
                    //*****************************************************************
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket((byte)Packet.JoinRoom);
                        writer.Write(player.ID);
                        writer.WriteIntExactly(room);
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
                        OnPlayerCreatedRoom?.Invoke(player, room);
                        //*******************************************************************************
                        using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                        {
                            writer.WritePacket((byte)Packet.CreateRoom);
                            writer.Write(player.ID);
                            writer.WriteIntExactly(room);
                            player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerCreatedRoom);
                        }
                        //*******************************************************************************
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
                                    Packet packet = (Packet)lastReader.ReadPacket();
                                    int _ID = lastReader.ReadInt32();
                                    byte[] sender = lastReader.ReadIntExactly();
                                    byte[] parameters = lastReader.ReadIntExactly();

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
                                                writer.WritePacket((byte)packet);
                                                writer.Write(_ID);
                                                writer.WriteIntExactly(sender);
                                                writer.WriteIntExactly(parameters);
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
                    writer.WritePacket((byte)Packet.Leave);
                    writer.WritePacket((byte)MatchmakingMode.Room);
                    writer.Write(player.ID);
                    writer.WriteIntExactly(player.Room);
                    player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerLeaveRoom);
                }
                //*******************************************************************************
                OnPlayerLeftRoom?.Invoke(player);
                //*******************************************************************************
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
                    writer.WritePacket((byte)Packet.Leave);
                    writer.WritePacket((byte)MatchmakingMode.Channel);
                    writer.Write(player.ID);
                    writer.WriteIntExactly(player.Channel);
                    player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerLeaveChannel);
                }
                //*******************************************************************************
                OnPlayerLeftChannel?.Invoke(player);
                //*******************************************************************************
                NeutronChannel channel = player.Channel;
                channel.Remove(player);
                player.Channel = null;
            }
            else
                player.Message(Packet.Leave, "ERROR: LeaveChannel Failed");
        }

        protected void DestroyPlayerHandler(NeutronPlayer player)
        {
            //MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.DestroyPlayer);
                player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerDestroyed);
            }
            OnPlayerDestroyed?.Invoke(player);
        }

        protected void SetPlayerPropertiesHandler(NeutronPlayer player, string properties)
        {
            if (OnPlayerPropertiesChanged.Invoke(player, properties))
            {
                player.Properties = properties;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.SetPlayerProperties);
                    writer.Write(player.ID);
                    player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerPropertiesChanged);
                }
            }
        }

        protected void SetRoomPropertiesHandler(NeutronPlayer player, string properties)
        {
            if (OnRoomPropertiesChanged.Invoke(player, properties))
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
                            writer.WritePacket((byte)Packet.SetRoomProperties);
                            writer.Write(player.ID);
                            player.Write(writer, OthersHelper.GetDefaultHandler().OnRoomPropertiesChanged);
                        }
                    }
                    else
                        player.Message(Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
                }
                else
                    player.Message(Packet.SetRoomProperties, "You are not inside a room.");
            }
        }

        protected void PingHandler(NeutronPlayer player, double time)
        {
            double diffTime = Math.Abs(Neutron.Time - time);
            if ((Neutron.Time > time) && diffTime > OthersHelper.GetConstants().TimeDesyncTolerance)
                Debug.LogError($"Jogador {player.Nickname} atraso em {diffTime} Ms!");
            else if ((time > Neutron.Time) && diffTime > OthersHelper.GetConstants().TimeDesyncTolerance)
                Debug.LogError($"Jogador {player.Nickname} adiantado em {diffTime} Ms!");
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.Ping);
                writer.Write(Neutron.Time);
                writer.Write(time);
                player.Write(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Tcp, Packet.Ping);
            }
        }

        protected void CustomPacketHandler(NeutronPlayer player, bool isMine, int viewId, byte[] parameters, CustomPacket packet, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            if (MatchmakingHelper.GetPlayer(viewId, out NeutronPlayer nPlayer))
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.CustomPacket);
                    writer.WritePacket((byte)packet);
                    writer.Write(player.ID);
                    writer.WriteIntExactly(parameters);

                    if (isMine)
                        nPlayer.Write(writer, targetTo, tunnelingTo, protocol);
                    else
                        nPlayer.Write(writer, TargetTo.Me, TunnelingTo.Me, protocol);
                }
            }
            else
                Debug.LogError("dsdsdsd");
        }

        protected void OnAutoSyncHandler(NeutronPacket packet, short viewId, byte instanceId, byte[] buffer, RegisterMode registerType)
        {
#if UNITY_EDITOR
            ThreadHelper.DoNotAllowSimultaneousAccess(PacketProcessingStack_ManagedThreadId);
#endif
            NeutronPlayer player = packet.Owner;
            void Run((int, int, RegisterMode) key)
            {
                void Send() => SocketHelper.Redirect(packet, MatchmakingHelper.GetTargetTo(packet.IsServerSide), MatchmakingHelper.Tunneling(player, TunnelingTo.Auto));
                if (MatchmakingHelper.GetNetworkObject(key, player, out NeutronView neutronView))
                {
                    if (neutronView.NeutronBehaviours.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                    {
                        using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                        {
                            reader.SetBuffer(buffer);
                            if (neutronBehaviour.OnAutoSynchronization(null, reader, false))
                                Send();
                        }
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
                    case RegisterMode.Scene:
                        Run((0, viewId, registerType));
                        break;
                    case RegisterMode.Player:
                        Run((viewId, viewId, registerType));
                        break;
                    case RegisterMode.Dynamic:
                        Run((player.ID, viewId, registerType));
                        break;
                }
            }
            else
                player.Message(Packet.OnAutoSync, "Have you ever joined a channel or room?");
        }
        #endregion
    }
}
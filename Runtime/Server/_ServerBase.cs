using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        #region Properties
        public static NeutronServer This {
            get;
            set;
        }

        /// <summary>
        ///* Obtenha o tempo atual em segundos(sec) desde do início da conexão.<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).<br/>
        ///* Não afetado pela rede.
        /// </summary>
#if UNITY_SERVER || UNITY_EDITOR
        public double LocalTime => This.Instance.NetworkTime.LocalTime;
#else
        public double LocalTime => 0;
#endif
        #endregion

        #region Events
        public static event NeutronEventNoReturn OnAwake;
        public static event NeutronEventNoReturn<NeutronPlayer> OnPlayerConnected;
        public static event NeutronEventNoReturn<NeutronPlayer> OnPlayerDisconnected;
        public static event NeutronEventWithReturn<NeutronPlayer, string, bool> OnPlayerNicknameChanged;
        public static event NeutronEventWithReturn<NeutronPlayer, string, bool> OnMessageReceived;
        public static event NeutronEventNoReturn<NeutronPlayer> OnPlayerDestroyed;
        public static event NeutronEventNoReturn<NeutronPlayer> OnPlayerJoinedChannel;
        public static event NeutronEventNoReturn<NeutronPlayer, NeutronRoom> OnPlayerJoinedRoom;
        public static event NeutronEventWithReturn<NeutronPlayer, NeutronRoom, bool> OnPlayerCreatedRoom;
        public static event NeutronEventNoReturn<NeutronPlayer> OnPlayerLeftChannel;
        public static event NeutronEventNoReturn<NeutronPlayer> OnPlayerLeftRoom;
        public static event NeutronEventWithReturn<NeutronPlayer, string, bool> OnPlayerPropertiesChanged;
        public static event NeutronEventWithReturn<NeutronPlayer, string, bool> OnRoomPropertiesChanged;
        public static event NeutronEventWithReturn<NeutronPlayer, Authentication, Task<bool>> OnAuthentication;
        #endregion

        #region Mono Behaviour
        protected override void Awake()
        {
            base.Awake();
            {
                This = (NeutronServer)this;
                //* Prepara os containers e aciona todos os eventos de criação.
                OnAwake?.Invoke();
            }
        }
        #endregion

        #region Handlers
        protected void DisconnectHandler(NeutronPlayer player)
        {
            using (player) //* Libera os recursos não gerenciados do jogador desconectado.
            {
                if (SocketHelper.RemovePlayerFromServer(player))
                    OnPlayerDisconnected?.Invoke(player);
            }
        }

        protected async void HandshakeHandler(NeutronPlayer player, double clientTime, Authentication authentication)
        {
            if (authentication.Pass.Decrypt(out string phrase))
            {
                authentication = new Authentication(authentication.User, phrase, false);
                try
                {
                    if (await OnAuthentication.Invoke(player, authentication)) //* First packet
                    {
                        using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull()) //* Second First packet
                        {
                            NeutronStream.IWriter writer = stream.Writer;
                            writer.WritePacket((byte)Packet.Handshake);
                            writer.Write(LocalTime);
                            writer.Write(clientTime);
                            writer.Write(player.StateObject.UdpLocalEndPoint.Port);
                            writer.WriteWithInteger(player);
                            player.Write(writer);
                        }
                        OnPlayerConnected?.Invoke(player); //* Three packet
                    }
                }
                catch (Exception ex) // Tasks manual catch exception.
                {
                    LogHelper.Stacktrace(ex);
                }
            }
            else if (!LogHelper.Error("Auth decrypt failed!"))
                DisconnectHandler(player);
        }

        protected void SynchronizeHandler(NeutronPlayer player, Protocol protocol)
        {
            //* Envia todos os jogadores conectados para mim.
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                NeutronPlayer[] players = MatchmakingHelper.Internal.Tunneling(player, TunnelingTo.Auto);
                players = players.Where(x => x.Id != player.Id).ToArray();
                writer.WritePacket((byte)Packet.Synchronize);
                writer.Write((byte)1);
                writer.WriteNext(players.Serialize().Compress(CompressionMode.Deflate));
                player.Write(writer, TargetTo.Me, TunnelingTo.Me, protocol);
            }

            //* Envia-me para todos os jogadores conectados.
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Synchronize);
                writer.Write((byte)2);
                writer.WriteNext(player.Serialize().Compress(CompressionMode.Deflate));
                player.Write(writer, TargetTo.Others, TunnelingTo.Auto, protocol);
            }
        }

        protected void SetNicknameHandler(NeutronPlayer player, string nickname)
        {
            if (OnPlayerNicknameChanged.Invoke(player, nickname))
            {
                player.Nickname = nickname;
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.Nickname);
                    writer.Write(nickname);
                    player.Write(writer, Helper.GetHandlers().OnPlayerNicknameChanged);
                }
            }
        }

        protected void ChatHandler(NeutronPlayer player, ChatMode packet, TunnelingTo tunnelingTo, int viewId, string message)
        {
            if (OnMessageReceived.Invoke(player, message))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.Chat);
                    writer.Write(message);
                    if (packet == ChatMode.Global)
                        player.Write(writer, TargetTo.All, tunnelingTo, Protocol.Tcp);
                    else if (packet == ChatMode.Private)
                    {
                        if (MatchmakingHelper.Server.GetPlayer(viewId, out NeutronPlayer playerFound))
                            playerFound.Write(player, writer, TargetTo.Me, TunnelingTo.Me, Protocol.Tcp);
                        else
                            player.Error(Packet.Chat, "Player not found!", ErrorMessage.PLAYER_NOT_FOUND);
                    }
                }
            }
        }

#pragma warning disable IDE1006
        protected void iRPCHandler(NeutronPlayer owner, NeutronPlayer sender, short viewId, byte rpcId, byte instanceId, byte[] buffer, RegisterMode registerType, TargetTo targetTo, CacheMode cache, Protocol protocol)
#pragma warning restore IDE1006
        {
            void Run((int, int, RegisterMode) key)
            {
                bool Send()
                {
                    TunnelingTo tunnelingTo = TunnelingTo.Auto;
                    if (targetTo == TargetTo.Me)
                        tunnelingTo = TunnelingTo.Me;
                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                    {
                        NeutronStream.IWriter writer = stream.Writer;
                        writer.WritePacket((byte)Packet.iRPC);
                        writer.WritePacket((byte)registerType);
                        writer.Write(viewId);
                        writer.Write(rpcId);
                        writer.Write(instanceId);
                        writer.WriteNext(buffer);
                        MatchmakingHelper.Internal.AddCache(rpcId, viewId, writer, owner, cache, CachedPacket.iRPC);
                        owner.Write(sender, writer, targetTo, tunnelingTo, protocol);
                    }
                    return true;
                }

                if (MatchmakingHelper.Server.GetNetworkObject(key, owner, out NeutronView neutronView))
                {
                    if (neutronView.iRPCs.TryGetValue((rpcId, instanceId), out RPCInvoker remoteProceduralCall))
                    {
                        try
                        {
                            iRPCAttribute iRPCAttribute = remoteProceduralCall.iRPC;
                            ReflectionHelper.iRPC(buffer, remoteProceduralCall, owner);
                            Send();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Stacktrace(ex);
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
                        Run((0, viewId, registerType));
                        break;
                    case RegisterMode.Player:
                        Run((viewId, viewId, registerType));
                        break;
                    case RegisterMode.Dynamic:
                        Run((owner.Id, viewId, registerType));
                        break;
                }
            }
            else
                owner.Error(Packet.iRPC, "Have you ever joined a channel or room?", ErrorMessage.MATCHMAKING_NOT_FOUND);
        }

#pragma warning disable IDE1006
        protected void gRPCHandler(NeutronPlayer owner, NeutronPlayer sender, byte id, byte[] buffer, Protocol protocol)
#pragma warning restore IDE1006
        {
#if UNITY_EDITOR
            ThreadManager.WarnSimultaneousAccess();
#endif
            bool Send(CacheMode cache, TargetTo targetTo, TunnelingTo tunnelingTo)
            {
#if UNITY_EDITOR
                ThreadManager.WarnSimultaneousAccess();
#endif
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.gRPC);
                    writer.Write(id);
                    writer.WriteNext(buffer);
                    MatchmakingHelper.Internal.AddCache(id, 0, writer, owner, cache, CachedPacket.gRPC);
                    owner.Write(sender, writer, targetTo, tunnelingTo, protocol);
                }
                return true;
            }

            if (GlobalBehaviour.gRPCs.TryGetValue(id, out RPCInvoker remoteProceduralCall))
            {
                try
                {
                    gRPCAttribute gRPCAttribute = remoteProceduralCall.gRPC;
                    ReflectionHelper.gRPC(owner, buffer, remoteProceduralCall, true, Neutron.Server.Instance.IsMine(owner), Neutron.Server.Instance);
                    Send(gRPCAttribute.Cache, gRPCAttribute.TargetTo, gRPCAttribute.TunnelingTo);
                }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                }
            }
            else
                owner.Error(Packet.gRPC, "Invalid gRPC ID, there is no attribute with this ID.", ErrorMessage.RPC_ID_NOT_FOUND);
        }

        protected void GetChannelsHandler(NeutronPlayer player)
        {
            if (!player.IsInMatchmaking())
            {
                NeutronChannel[] channels = ChannelsById.Values.ToArray();
                if (channels.Length > 0)
                {
                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                    {
                        NeutronStream.IWriter writer = stream.Writer;
                        writer.WritePacket((byte)Packet.GetChannels);
                        writer.WriteWithInteger(channels);
                        player.Write(writer);
                    }
                }
                else
                    player.Error(Packet.GetChannels, "No channels found!", ErrorMessage.CHANNELS_NOT_FOUND);
            }
            else
                player.Error(Packet.GetChannels, "You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.", ErrorMessage.MATCHMAKING_INDISPONIBLE);
        }

        protected void GetRoomsHandler(NeutronPlayer player)
        {
            if (player.IsInChannel() && !player.IsInRoom())
            {
                NeutronChannel channel = player.Channel;
                if (channel.RoomCount > 0)
                {
                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                    {
                        NeutronStream.IWriter writer = stream.Writer;
                        NeutronRoom[] rooms = channel.GetRooms(x => x.IsVisible);
                        writer.WritePacket((byte)Packet.GetRooms);
                        writer.WriteWithInteger(rooms);
                        player.Write(writer);
                    }
                }
                else
                    player.Error(Packet.GetRooms, "No rooms found!", ErrorMessage.ROOMS_NOT_FOUND);
            }
            else
                player.Error(Packet.GetRooms, "You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.", ErrorMessage.MATCHMAKING_INDISPONIBLE);
        }

        protected void JoinChannelHandler(NeutronPlayer player, int channelId)
        {
            if (!player.IsInChannel())
            {
                if (ChannelsById.Count > 0)
                {
                    if (ChannelsById.TryGetValue(channelId, out NeutronChannel channel))
                    {
                        if (channel.Add(player))
                        {
                            player.Channel = channel;
                            player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                            OnPlayerJoinedChannel?.Invoke(player);
                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                            {
                                NeutronStream.IWriter writer = stream.Writer;
                                writer.WritePacket((byte)Packet.JoinChannel);
                                writer.WriteWithInteger(channel);
                                player.Write(writer, TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
                            }
                        }
                        else
                            player.Error(Packet.JoinChannel, "Failed to join channel, unknow issue ):", ErrorMessage.FAILED_TO_JOIN_MATCHMAKING);
                    }
                    else
                        player.Error(Packet.JoinChannel, $"We couldn't find a channel with this Id: {channelId}", ErrorMessage.FAILED_TO_JOIN_MATCHMAKING);
                }
                else
                    player.Error(Packet.JoinChannel, "There are no channels created on the server.", ErrorMessage.CHANNELS_NOT_FOUND);
            }
            else
                player.Error(Packet.JoinChannel, "You are already joined to a channel.", ErrorMessage.MATCHMAKING_INDISPONIBLE);
        }

        protected void JoinRoomHandler(NeutronPlayer player, int roomId, string password)
        {
            if (player.IsInChannel() && !player.IsInRoom())
            {
                NeutronRoom room = player.Channel.GetRoom(roomId);
                if (room != null)
                {
                    if (room.Password == password)
                    {
                        if (room.Add(player))
                        {
                            player.Room = room;
                            player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                            OnPlayerJoinedRoom?.Invoke(player, room);
                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                            {
                                NeutronStream.IWriter writer = stream.Writer;
                                writer.WritePacket((byte)Packet.JoinRoom);
                                writer.WriteWithInteger(room);
                                player.Write(writer, Helper.GetHandlers().OnPlayerJoinedRoom);
                            }
                        }
                        else
                            player.Error(Packet.JoinRoom, "Failed to add player.", ErrorMessage.FAILED_TO_JOIN_MATCHMAKING);
                    }
                    else
                        player.Error(Packet.JoinRoom, "Failed to join in room, incorrect password", ErrorMessage.FAILED_TO_JOIN_MATCHMAKING_WRONG_PASSWORD);
                }
                else
                    player.Error(Packet.JoinChannel, $"We couldn't find a room with this Id: {roomId}", ErrorMessage.FAILED_TO_JOIN_MATCHMAKING);
            }
            else
                player.Error(Packet.JoinRoom, "You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.", ErrorMessage.MATCHMAKING_INDISPONIBLE);
        }

        protected void CreateRoomHandler(NeutronPlayer player, NeutronRoom room, string password)
        {
            if (player.IsInChannel() && !player.IsInRoom())
            {
                try
                {
                    NeutronChannel channel = player.Channel;
                    if (room.Id == 0)
                        room.Id = Helper.GetAvailableId(channel.GetRooms(), x => x.Id, channel.MaxRooms);
                    room.Password = password;
                    room.Owner = player;

                    if (OnPlayerCreatedRoom.Invoke(player, room))
                    {
                        if (!string.IsNullOrEmpty(room.Name))
                        {
                            if (channel.Add(room))
                            {
                                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                {
                                    NeutronStream.IWriter writer = stream.Writer;
                                    writer.WritePacket((byte)Packet.CreateRoom);
                                    writer.WriteWithInteger(room);
                                    player.Write(writer, Helper.GetHandlers().OnPlayerCreatedRoom);
                                }
                                JoinRoomHandler(player, room.Id, password);
                            }
                            else
                                player.Error(Packet.CreateRoom, "Failed to create room!", ErrorMessage.FAILED_CREATE_ROOM);
                        }
                        else
                            player.Error(Packet.CreateRoom, "Room name is null or empty!", ErrorMessage.IS_NULL_OR_EMPTY);
                    }
                }
                catch (Exception ex) // Tasks manual catch exception.
                {
                    LogHelper.Stacktrace(ex);
                }
            }
            else
                player.Error(Packet.CreateRoom, "You cannot create a room by being inside one.\r\nCall LeaveRoom or you not within a channel!", ErrorMessage.MATCHMAKING_INDISPONIBLE);
        }

        protected void GetCacheHandler(NeutronPlayer player, CachedPacket cachedPacket, byte Id, bool isOwner)
        {
            INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
            if (neutronMatchmaking != null)
            {
                foreach (var cache in neutronMatchmaking.Caches())
                {
                    if (!isOwner && cache.Owner.Equals(player))
                        continue;

                    if (cachedPacket != CachedPacket.All)
                    {
                        switch (Id)
                        {
                            case 0:
                                {
                                    if (cache.Packet == cachedPacket)
                                        Send(cache.Buffer, cache.Owner);
                                    else
                                        continue;
                                }
                                break;
                            default:
                                {
                                    if (cache.Packet == cachedPacket && cache.Id == Id)
                                        Send(cache.Buffer, cache.Owner);
                                    else
                                        continue;
                                }
                                break;
                        }
                    }
                    else
                        Send(cache.Buffer, cache.Owner);
                }
            }

            void Send(byte[] buffer, NeutronPlayer owner) => player.Write(owner, buffer);
        }

        protected void LeaveRoomHandler(NeutronPlayer player)
        {
            if (player.IsInRoom())
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.Leave);
                    writer.WritePacket((byte)MatchmakingMode.Room);
                    writer.WriteWithInteger(player.Room);
                    player.Write(writer, Helper.GetHandlers().OnPlayerLeaveRoom);
                }
                OnPlayerLeftRoom?.Invoke(player);

                INeutronMatchmaking matchmaking = player.Matchmaking;
                if (matchmaking != null)
                {
                    if (matchmaking.Remove(player))
                        MatchmakingHelper.Internal.Leave(player, MatchmakingMode.Room);
                }
            }
            else
                player.Error(Packet.Leave, "Leave Room Failed", ErrorMessage.FAILED_LEAVE_MATCHMAKING);
        }

        protected void LeaveChannelHandler(NeutronPlayer player)
        {
            if (player.IsInChannel())
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.Leave);
                    writer.WritePacket((byte)MatchmakingMode.Channel);
                    writer.WriteWithInteger(player.Channel);
                    player.Write(writer, Helper.GetHandlers().OnPlayerLeaveChannel);
                }
                OnPlayerLeftChannel?.Invoke(player);
                NeutronChannel channel = player.Channel;
                channel.Remove(player);
                player.Channel = null;
            }
            else
                player.Error(Packet.Leave, "Leave Channel Failed", ErrorMessage.FAILED_LEAVE_MATCHMAKING);
        }

        protected void DestroyPlayerHandler(NeutronPlayer player)
        {
            //MatchmakingHelper.DestroyPlayer(nSender);
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.Destroy);
                player.Write(writer, Helper.GetHandlers().OnPlayerDestroyed);
            }
            OnPlayerDestroyed?.Invoke(player);
        }

        protected void SetPlayerPropertiesHandler(NeutronPlayer player, string properties)
        {
            if (OnPlayerPropertiesChanged.Invoke(player, properties))
            {
                player.Properties = properties;
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.SetPlayerProperties);
                    writer.Write(properties);
                    player.Write(writer, Helper.GetHandlers().OnPlayerPropertiesChanged);
                }
            }
        }

        protected void SetRoomPropertiesHandler(NeutronPlayer player, string properties)
        {
            if (OnRoomPropertiesChanged.Invoke(player, properties))
            {
                if (player.IsInRoom())
                {
                    NeutronRoom room = player.Room;
                    room.Properties = properties;
                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                    {
                        NeutronStream.IWriter writer = stream.Writer;
                        writer.WritePacket((byte)Packet.SetRoomProperties);
                        writer.Write(properties);
                        player.Write(writer, Helper.GetHandlers().OnRoomPropertiesChanged);
                    }
                }
                else
                    player.Error(Packet.SetRoomProperties, "You are not inside a room.", ErrorMessage.MATCHMAKING_NOT_FOUND);
            }
        }

        protected void PingHandler(NeutronPlayer player, double time)
        {
            double diffTime = Math.Abs(LocalTime - time);
            //if ((Time > time) && diffTime > OthersHelper.GetConstants().TimeDesyncTolerance)
            //    Debug.LogError($"Jogador {player.Nickname} atraso em {diffTime} Ms!");
            //else if ((time > Time) && diffTime > OthersHelper.GetConstants().TimeDesyncTolerance)
            //    Debug.LogError($"Jogador {player.Nickname} adiantado em {diffTime} Ms!");
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.UdpKeepAlive);
                writer.Write(LocalTime);
                writer.Write(time);
                player.Write(writer, TargetTo.Me, TunnelingTo.Me, Protocol.Udp);
            }
        }

        protected void CustomPacketHandler(NeutronPlayer player, bool isMine, int viewId, byte[] parameters, byte packet, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            if (MatchmakingHelper.Server.GetPlayer(viewId, out NeutronPlayer nPlayer))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    NeutronStream.IWriter writer = stream.Writer;
                    writer.WritePacket((byte)Packet.CustomPacket);
                    writer.WritePacket(packet);
                    writer.WriteWithInteger(parameters);
                    if (isMine)
                        nPlayer.Write(writer, targetTo, tunnelingTo, protocol);
                    else
                        nPlayer.Write(writer, TargetTo.Me, TunnelingTo.Me, protocol);
                }
            }
            else
                player.Error(Packet.CustomPacket, "Player not found!", ErrorMessage.PLAYER_NOT_FOUND);
        }

        protected void OnAutoSyncHandler(NeutronPacket packet, short viewId, byte instanceId, byte[] buffer, RegisterMode registerType)
        {
            NeutronPlayer player = packet.Owner;
            void Run((int, int, RegisterMode) key)
            {
                void Send() => MatchmakingHelper.Internal.Redirect(packet, MatchmakingHelper.Internal.GetTargetTo(packet.IsServerSide), MatchmakingHelper.Internal.Tunneling(player, TunnelingTo.Auto));
                if (MatchmakingHelper.Server.GetNetworkObject(key, player, out NeutronView neutronView))
                {
                    if (neutronView.NeutronBehaviours.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                    {
                        using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                        {
                            NeutronStream.IReader reader = stream.Reader;
                            reader.SetBuffer(buffer);
                            if (neutronBehaviour.OnAutoSynchronization(stream, false))
                                Send();
                        }
                    }
                    else
                        LogHelper.Error(Packet.AutoSync, "Auto Sync instance not found!");
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
                        Run((player.Id, viewId, registerType));
                        break;
                }
            }
            else
                player.Error(Packet.AutoSync, "Have you ever joined a channel or room?", ErrorMessage.MATCHMAKING_NOT_FOUND);
        }
        #endregion
    }
}
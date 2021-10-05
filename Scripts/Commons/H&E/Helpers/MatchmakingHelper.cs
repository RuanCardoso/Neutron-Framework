using NeutronNetwork.Extensions;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using NeutronNetwork.Server.Internal;
using System;
using System.Linq;

namespace NeutronNetwork.Helpers
{
    /// <summary>
    ///* Possui algumas funções de uso interno e público para o lado do servidor ou o lado do cliente.
    /// </summary>
    public static class MatchmakingHelper
    {
        /// <summary>
        ///* Todas as funções aqui disposta são de uso interno, nada o impede de usar, mas saiba oque está fazendo.
        /// </summary>
        public static class Internal
        {
            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN<br/>
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static void Leave(NeutronPlayer player, MatchmakingMode matchmakingMode)
            {
                if (matchmakingMode == MatchmakingMode.Channel)
                    player.Channel = null;
                if (matchmakingMode == MatchmakingMode.Room)
                    player.Room = null;
                player.Matchmaking = Matchmaking(player);
            }

            /// <summary>
            ///* Disponível somente ao lado do servidor.<br/>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static bool AddPlayer(NeutronPlayer player)
            {
                return Neutron.Server.PlayersById.TryAdd(player.ID, player);
            }

            /// <summary>
            ///* Disponível somente ao lado do servidor.<br/>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static TargetTo GetTargetTo(bool isServerSide)
            {
                return isServerSide ? TargetTo.All : TargetTo.Others;
            }

            /// <summary>
            ///* Disponível somente ao lado do servidor.<br/>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static void AddCache(int id, int viewId, NeutronStream.IWriter writer, NeutronPlayer player, CacheMode cache, CachedPacket cachedPacket)
            {
                //LogHelper.Error(ThreadHelper.GetThreadID());
                if (cache != CacheMode.None)
                {
                    INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
                    if (neutronMatchmaking != null)
                    {
                        NeutronCache dataCache = new NeutronCache(id, writer.ToArray(), player, cachedPacket, cache);
                        neutronMatchmaking.Add(dataCache, viewId);
                    }
                    else
                        LogHelper.Error("Cache: Matchmaking not found!");
                }
            }

            /// <summary>
            ///* Disponível somente ao lado do servidor.<br/>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static NeutronPlayer[] Tunneling(NeutronPlayer player, TunnelingTo tunnelingTo)
            {
                switch (tunnelingTo)
                {
                    case TunnelingTo.Me:
                        return null;
                    case TunnelingTo.Server:
                        {
                            return Neutron.Server.PlayersBySocket.Values.ToArray();
                        }
                    case TunnelingTo.Channel:
                        {
                            if (player.IsInChannel())
                                return player.Channel.Players();
                            else
                                LogHelper.Error("Failed to direct packet, channel not found. Join a channel before sending the packet.");
                            return default;
                        }
                    case TunnelingTo.Room:
                        {
                            if (player.IsInRoom())
                                return player.Room.Players();
                            else
                                LogHelper.Error("Failed to direct packet, room not found. Join a room before sending the packet.");
                            return default;
                        }
                    case TunnelingTo.Auto:
                        {
                            INeutronMatchmaking matchmaking = player.Matchmaking;
                            if (matchmaking != null)
                                return matchmaking.Players();
                            else
                                return Tunneling(player, TunnelingTo.Server);
                        }
                    default:
                        return ServerBase.OnCustomTunneling?.Invoke(player, tunnelingTo);
                }
            }

            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static void Redirect(NeutronPacket packet, TargetTo targetTo, NeutronPlayer[] players)
            {
                switch (targetTo)
                {
                    case TargetTo.Me:
                        {
                            if (!packet.Owner.IsServerPlayer)
                                Neutron.Server.OnSendingData(packet.Owner, packet);
                            else
                                throw new Exception("The Server cannot transmit data to itself.");
                        }
                        break;
                    case TargetTo.Server:
                        //* Servidor não redireciona pacote, somente ele recebe (:
                        //* Redirecionamento manual?
                        break;
                    case TargetTo.All:
                        {
                            for (int i = 0; i < players.Length; i++)
                                Neutron.Server.OnSendingData(players[i], packet);
                        }
                        break;
                    case TargetTo.Others:
                        {
                            for (int i = 0; i < players.Length; i++)
                            {
                                if (players[i].Equals(packet.Owner))
                                    continue;
                                Neutron.Server.OnSendingData(players[i], packet);
                            }
                        }
                        break;
                    default:
                        ServerBase.OnCustomTarget?.Invoke(packet.Owner, packet, targetTo, players);
                        break;
                }
                packet.Recycle();
            }

            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static void Redirect(NeutronPlayer owner, NeutronPlayer sender, Protocol protocol, TargetTo targetTo, byte[] buffer, NeutronPlayer[] players)
            {
                NeutronPacket packet = new NeutronPacket(buffer, owner, sender, protocol);
                switch (targetTo)
                {
                    case TargetTo.Me:
                        {
                            if (!owner.IsServerPlayer)
                                Neutron.Server.OnSendingData(owner, packet);
                            else
                                throw new Exception("The Server cannot transmit data to itself.");
                        }
                        break;
                    case TargetTo.Server:
                        //* Servidor não redireciona pacote, somente ele recebe (:
                        //* Redirecionamento manual?
                        break;
                    case TargetTo.All:
                        {
                            for (int i = 0; i < players.Length; i++)
                                Neutron.Server.OnSendingData(players[i], packet);
                        }
                        break;
                    case TargetTo.Others:
                        {
                            for (int i = 0; i < players.Length; i++)
                            {
                                if (players[i].Equals(owner))
                                    continue;
                                Neutron.Server.OnSendingData(players[i], packet);
                            }
                        }
                        break;
                    default:
                        ServerBase.OnCustomTarget?.Invoke(owner, packet, targetTo, players);
                        break;
                }
                packet.Recycle();
            }
        }
        /// <summary>
        ///* Todas as funções aqui disposta são de uso ao lado do servidor.
        /// </summary>
        public static class Server
        {
            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [ThreadSafe]
            public static bool GetPlayer(int id, out NeutronPlayer player)
            {
                return Neutron.Server.PlayersById.TryGetValue(id, out player);
            }

            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [ThreadSafe]
            public static void BeginSender(NeutronPlayer sender)
            {
                lock (Encapsulate.BeginLock)
                {
                    if (Encapsulate.Sender == null)
                        Encapsulate.Sender = sender;
                    else
                        LogHelper.Error("it is necessary to call \"EndSender\" first!");
                }
            }

            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [ThreadSafe]
            public static void EndSender()
            {
                lock (Encapsulate.BeginLock)
                {
                    if (Encapsulate.Sender != null)
                        Encapsulate.Sender = null;
                    else
                        LogHelper.Error("it is necessary to call \"BeginSender\" first!");
                }
            }

            /// <summary>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
            /// </summary>
            [ThreadSafe]
            public static bool GetNetworkObject((int, int, RegisterMode) id, NeutronPlayer player, out NeutronView view)
            {
                view = null;
                INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
                if (neutronMatchmaking != null)
                    return neutronMatchmaking.Views.TryGetValue(id, out view);
                else
                    return false;
            }
        }

        /// <summary>
        ///* Retorna o matchmaking do jogador.<br/>
        /// </summary>
        [ThreadSafe]
        public static INeutronMatchmaking Matchmaking(NeutronPlayer player)
        {
            if (player.IsInChannel())
            {
                if (player.IsInRoom())
                    return player.Room;
                else
                    return player.Channel;
            }
            else
                return default;
        }

        /// <summary>
        ///* Remove todos os objetos de rede do jogador.
        /// </summary>
        public static void Destroy(NeutronPlayer player)
        {
            var @event = player.OnDestroy;
            @event?.Invoke();
        }
    }
}
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using NeutronNetwork.Server.Internal;
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
            ///* Disponível somente ao lado do servidor.<br/>
            ///* Recomendado o uso dos pré-processadores #if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN<br/>
            /// </summary>
            [Internal]
            [ThreadSafe]
            public static void Leave(NeutronPlayer player, bool leaveRoom = true, bool leaveChannel = true)
            {
                if (leaveChannel)
                    player.Channel = null;
                if (leaveRoom)
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
            public static void AddCache(int id, int viewId, NeutronWriter writer, NeutronPlayer player, CacheMode cache, CachedPacket cachedPacket)
            {
                if (cache != CacheMode.None)
                {
                    INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
                    if (neutronMatchmaking != null)
                    {
                        NeutronCache dataCache = new NeutronCache(id, writer.ToArray(), player, cachedPacket, cache);
                        neutronMatchmaking.Add(dataCache, viewId);
                    }
                    else
                        LogHelper.Error("Cache -> Invalid matchmaking");
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
                            {
                                player.Message(Packet.Empty, "Failed to direct packet, channel not found. Join a channel before sending the packet.");
                                return null;
                            }
                        }
                    case TunnelingTo.Room:
                        {
                            if (player.IsInRoom())
                                return player.Room.Players();
                            else
                            {
                                player.Message(Packet.Empty, "Failed to direct packet, room not found. Join a room before sending the packet.");
                                return null;
                            }
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
                    return neutronMatchmaking.SceneView.Views.TryGetValue(id, out view);
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
                return null;
        }
    }
}
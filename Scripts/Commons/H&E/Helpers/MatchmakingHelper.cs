using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Server;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Helpers
{
    public static class MatchmakingHelper
    {
        public static bool GetPlayer(int id, out NeutronPlayer player)
        {
            return Neutron.Server.PlayersById.TryGetValue(id, out player);
        }

        public static bool AddPlayer(NeutronPlayer player)
        {
            return Neutron.Server.PlayersById.TryAdd(player.ID, player);
        }

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

        public static void Leave(NeutronPlayer player, bool leaveRoom = true, bool leaveChannel = true)
        {
            if (leaveChannel)
                player.Channel = null;
            if (leaveRoom)
                player.Room = null;
            player.Matchmaking = Matchmaking(player);
        }

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

        public static TargetTo TargetTo(bool isServerSide)
        {
            return isServerSide ? global::TargetTo.All : global::TargetTo.Others;
        }

        public static bool GetNetworkObject((int, int, RegisterType) id, NeutronPlayer player, out NeutronView view)
        {
            view = null;
            INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
            if (neutronMatchmaking != null)
                return neutronMatchmaking.SceneView.Views.TryGetValue(id, out view);
            else
                return false;
        }

        public static void AddCache(int id, int viewId, NeutronWriter writer, NeutronPlayer player, Cache cache, CachedPacket cachedPacket)
        {
            if (cache != Cache.None)
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
}
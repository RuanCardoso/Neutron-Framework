using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Interfaces;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Helpers
{
    public static class MatchmakingHelper
    {
        #region Events
        public static NeutronEventWithReturn<NeutronPlayer, TunnelingTo, NeutronPlayer[]> OnCustomBroadcast;
        #endregion

        public static bool GetPlayer(int id, out NeutronPlayer player)
        {
            return Neutron.Server.PlayersById.TryGetValue(id, out player);
        }

        public static bool AddPlayer(NeutronPlayer player)
        {
            return Neutron.Server.PlayersById.TryAdd(player.ID, player);
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

        public static bool GetNetworkObject((int, int) id, NeutronPlayer player, out NeutronView view)
        {
            view = null;
            INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
            if (neutronMatchmaking != null)
                return neutronMatchmaking.SceneView.Views.TryGetValue(id, out view);
            else
                return false;
        }

        public static void SetCache(int id, byte[] buffer, NeutronPlayer player, Cache cache, CachedPacket packet)
        {
            if (cache != Cache.None)
            {
                INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
                if (neutronMatchmaking != null)
                {
                    NeutronCache l_Cache = new NeutronCache(id, buffer, player, packet, cache);
                    if (l_Cache != null)
                        neutronMatchmaking.Add(l_Cache);
                }
            }
        }

        public static NeutronPlayer[] Tunneling(NeutronPlayer player, TunnelingTo tunnelingTo)
        {
            INeutronMatchmaking neutronMatchmaking = player.Matchmaking;
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
                            return null;
                    }
                case TunnelingTo.Room:
                    {
                        if (player.IsInRoom())
                            return player.Room.Players();
                        else
                            return null;
                    }
                case TunnelingTo.Auto:
                    {
                        if (neutronMatchmaking != null)
                            return neutronMatchmaking.Players();
                        else
                            return Neutron.Server.PlayersBySocket.Values.ToArray();
                    }
                default:
                    return OnCustomBroadcast.Invoke(player, tunnelingTo);
            }
        }
    }
}
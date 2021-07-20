using System.Linq;
using NeutronNetwork;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Server.Internal;

namespace NeutronNetwork.Helpers
{
    public static class MatchmakingHelper
    {
        #region Events
        public static NeutronEventWithReturn<NeutronPlayer[], NeutronPlayer, TunnelingTo> OnCustomBroadcast = new NeutronEventWithReturn<NeutronPlayer[], NeutronPlayer, TunnelingTo>();
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
                player.CurrentChannel = -1;
            if (leaveRoom)
                player.CurrentRoom = -1;
            player.Matchmaking = Matchmaking(player);
        }

        public static INeutronMatchmaking Matchmaking(NeutronPlayer player)
        {
            if (player.IsInChannel())
            {
                if (Neutron.Server.ChannelsById.TryGetValue(player.CurrentChannel, out NeutronChannel l_Channel))
                    if (player.IsInRoom())
                        return l_Channel.GetRoom(player.CurrentRoom);
                    else
                        return l_Channel;
                else
                    return null;
            }
            else
                return null;
        }

        public static bool GetNetworkObject(int nID, NeutronPlayer nPlayer, out NeutronView nView)
        {
            nView = null;
            INeutronMatchmaking neutronMatchmaking = nPlayer.Matchmaking;
            if (neutronMatchmaking != null)
                return neutronMatchmaking.SceneSettings.networkObjects.TryGetValue(nID, out nView);
            else return false;
        }

        public static void SetCache(int attributeID, byte[] buffer, NeutronPlayer nOwner, Cache cacheMode, CachedPacket packet)
        {
            if (cacheMode != Cache.None)
            {
                INeutronMatchmaking neutronMatchmaking = nOwner.Matchmaking;
                if (neutronMatchmaking != null)
                {
                    NeutronCache l_Cache = new NeutronCache(attributeID, buffer, nOwner, packet, cacheMode);
                    if (l_Cache != null)
                        neutronMatchmaking.AddCache(l_Cache);
                }
            }
        }

        public static NeutronPlayer[] Tunneling(NeutronPlayer player, TunnelingTo broadcast)
        {
            INeutronMatchmaking matchmaking = player.Matchmaking;
            switch (broadcast)
            {
                case TunnelingTo.Me:
                    return null;
                case TunnelingTo.Server:
                    {
                        return Neutron.Server.PlayersBySocket.Values.ToArray();
                    }
                case TunnelingTo.Channel:
                    {
                        if (Neutron.Server.ChannelsById.TryGetValue(player.CurrentChannel, out NeutronChannel l_Channel))
                            return
                                l_Channel.GetPlayers();
                        else
                            return null;
                    }
                case TunnelingTo.Room:
                    {
                        if (Neutron.Server.ChannelsById.TryGetValue(player.CurrentChannel, out NeutronChannel l_Channel))
                        {
                            NeutronRoom l_Room = l_Channel.GetRoom(player.CurrentRoom);
                            if (l_Room != null)
                                return l_Room.GetPlayers();
                            else
                                return null;
                        }
                        else
                            return null;
                    }
                case TunnelingTo.Auto:
                    {
                        if (matchmaking != null)
                            return matchmaking.GetPlayers();
                        else
                            return Neutron.Server.PlayersBySocket.Values.ToArray();
                    }
                default:
                    return OnCustomBroadcast.Invoke(player, broadcast).Result;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server.Delegates;
using UnityEngine;

public static class MatchmakingHelper
{
    #region Events
    public static event Events.OnCustomBroadcast m_OnCustomBroadcast;
    #endregion

    public static bool GetPlayer(int nID, out Player nPlayer)
    {
        return Neutron.Server.PlayersById.TryGetValue(nID, out nPlayer);
    }

    public static bool AddPlayer(Player nPlayer)
    {
        return Neutron.Server.PlayersById.TryAdd(nPlayer.ID, nPlayer);
    }

    public static void DestroyPlayer(Player nPlayer)
    {
        if (nPlayer.NeutronView != null)
        {
            NeutronDispatcher.Dispatch(() =>
            {
                UnityEngine.Object.Destroy(nPlayer.NeutronView.gameObject);
            });
        }
    }

    public static void Leave(Player nPlayer, bool leaveRoom = true, bool leaveChannel = true)
    {
        if (leaveChannel) nPlayer.CurrentChannel = -1;
        if (leaveRoom) nPlayer.CurrentRoom = -1;
    }

    public static INeutronMatchmaking Matchmaking(Player nPlayer)
    {
        if (nPlayer.IsInChannel())
        {
            if (Neutron.Server.ChannelsById.TryGetValue(nPlayer.CurrentChannel, out Channel l_Channel))
                if (nPlayer.IsInRoom())
                    return l_Channel.GetRoom(nPlayer.CurrentRoom);
                else return l_Channel;
            else return null;
        }
        else return null;
    }

    public static bool GetNetworkObject(int nID, Player nPlayer, out NeutronView nView)
    {
        nView = null;
        INeutronMatchmaking neutronMatchmaking = Matchmaking(nPlayer);
        if (neutronMatchmaking != null)
            return neutronMatchmaking.SceneSettings.networkObjects.TryGetValue(nID, out nView);
        else return false;
    }

    public static void SetCache(int attributeID, byte[] buffer, Player nOwner, CacheMode cacheMode, CachedPacket packet)
    {
        if (cacheMode != CacheMode.None)
        {
            INeutronMatchmaking neutronMatchmaking = MatchmakingHelper.Matchmaking(nOwner);
            if (neutronMatchmaking != null)
            {
                CachedBuffer l_Cache = new CachedBuffer(attributeID, buffer, nOwner, packet, cacheMode);
                if (l_Cache != null)
                    neutronMatchmaking.AddCache(l_Cache);
            }
        }
    }

    public static Player[] Broadcast(Player nPlayer, Broadcast broadcast)
    {
        INeutronMatchmaking matchmaking = MatchmakingHelper.Matchmaking(nPlayer);
        switch (broadcast)
        {
            case global::Broadcast.Me:
                return null;
            case global::Broadcast.Server:
                {
                    return Neutron.Server.PlayersBySocket.Values.ToArray();
                }
            case global::Broadcast.Channel:
                {
                    if (Neutron.Server.ChannelsById.TryGetValue(nPlayer.CurrentChannel, out Channel l_Channel))
                        return l_Channel.GetPlayers();
                    else return null;
                }
            case global::Broadcast.Room:
                {
                    if (Neutron.Server.ChannelsById.TryGetValue(nPlayer.CurrentChannel, out Channel l_Channel))
                    {
                        Room l_Room = l_Channel.GetRoom(nPlayer.CurrentRoom);
                        if (l_Room != null)
                            return l_Room.GetPlayers();
                        else return null;
                    }
                    else return null;
                }
            case global::Broadcast.Auto:
                {
                    if (matchmaking != null)
                        return matchmaking.GetPlayers();
                    else return Neutron.Server.PlayersBySocket.Values.ToArray();
                }
            default:
                return m_OnCustomBroadcast?.Invoke(nPlayer, broadcast);
        }
    }
}
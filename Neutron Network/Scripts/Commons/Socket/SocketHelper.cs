using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;
using UnityEngine;

public static class SocketHelper
{
    public static bool GetPlayer(TcpClient nSocket, out Player nPlayer)
    {
        return Neutron.Server.PlayersBySocket.TryGetValue(nSocket, out nPlayer);
    }

    public static bool AddPlayer(Player nPlayer)
    {
        return Neutron.Server.PlayersBySocket.TryAdd(nPlayer.tcpClient, nPlayer)
            && MatchmakingHelper.AddPlayer(nPlayer);
    }

    public static bool RemovePlayerFromServer(Player nPlayer)
    {
        bool tryRemove = Neutron.Server.PlayersBySocket.TryRemove(nPlayer.tcpClient, out Player removedPlayerBySocket)
            && Neutron.Server.PlayersById.TryRemove(nPlayer.ID, out Player _);
        if (tryRemove)
        {
            string addr = nPlayer.RemoteEndPoint().Address.ToString();
            if (Neutron.Server.RegisteredConnectionsByIp.TryGetValue(addr, out int value))
                Neutron.Server.RegisteredConnectionsByIp[addr] = --value;
            MatchmakingHelper.DestroyPlayer(nPlayer);
            Disconnect(nPlayer);
            if (nPlayer.IsInRoom())
            {
                INeutronMatchmaking matchmaking = MatchmakingHelper.Matchmaking(nPlayer);
                if (matchmaking != null)
                {
                    if (matchmaking.RemovePlayer(nPlayer))
                        MatchmakingHelper.Leave(nPlayer, leaveChannel: false);
                    if (nPlayer.IsInChannel())
                    {
                        matchmaking = MatchmakingHelper.Matchmaking(nPlayer);
                        if (matchmaking.RemovePlayer(nPlayer))
                            MatchmakingHelper.Leave(nPlayer, leaveRoom: false);
                    }
                }
            }
            else
            {
                INeutronMatchmaking matchmaking = MatchmakingHelper.Matchmaking(nPlayer);
                if (matchmaking != null)
                {
                    if (matchmaking.RemovePlayer(nPlayer))
                        MatchmakingHelper.Leave(nPlayer, leaveRoom: false);
                }
            }
        }
        return tryRemove;
    }

    public static void Disconnect(Player nPlayer)
    {
        var handle = NeutronConfig.Settings.HandleSettings.OnPlayerDisconnected;
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.PlayerDisconnected);
            writer.WriteExactly<Player>(nPlayer);
            nPlayer.Send(writer, handle);
        }
    }

    public static void Redirect(Player nSender, Protocol nProtocol, SendTo nSendTo, byte[] nBuffer, Player[] nPlayers)
    {
        DataBuffer dataBuffer = new DataBuffer(nBuffer, nProtocol);
        if (dataBuffer != null)
        {
            switch (nSendTo)
            {
                case SendTo.Me:
                    if (!nSender.isServer)
                        nSender.qData.SafeEnqueue(dataBuffer);
                    else NeutronUtils.LoggerError("The Server cannot transmit data to itself.");
                    break;
                case SendTo.All:
                    if (nPlayers != null)
                    {
                        for (int i = 0; i < nPlayers.Length; i++)
                        {
                            if (nSender.IsBot)
                                if (!nPlayers[i].Equals(nSender) && nPlayers[i].IsBot) continue;
                            nPlayers[i].qData.SafeEnqueue(dataBuffer);
                        }
                    }
                    else NeutronUtils.LoggerError("The Server cannot transmit all data to nothing.");
                    break;
                case SendTo.Others:
                    if (nPlayers != null)
                    {
                        for (int i = 0; i < nPlayers.Length; i++)
                        {
                            if (nPlayers[i].Equals(nSender)) continue;
                            else if (nSender.IsBot && nPlayers[i].IsBot) continue;
                            nPlayers[i].qData.SafeEnqueue(dataBuffer);
                        }
                    }
                    else NeutronUtils.LoggerError("The Server cannot transmit others data to nothing.");
                    break;
            }
        }
    }

    public static void Dispose()
    {
        var l_Players = Neutron.Server.PlayersBySocket.Values.ToList();
        foreach (var p_Player in l_Players)
            p_Player.Dispose();
        Neutron.Server.TcpSocket.Stop();
    }
}
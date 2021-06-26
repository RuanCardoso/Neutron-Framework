using NeutronNetwork;
using NeutronNetwork.Helpers;
using NeutronNetwork.Server.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Extensions
{
    public static class MatchmakingExtesions
    {
        public static void Send(this Player mSender, NeutronWriter writer, SendTo sendTo, Broadcast broadcast, Protocol protocolType)
        {
            SocketHelper.Redirect(mSender, protocolType, sendTo, writer.ToArray(), MatchmakingHelper.Broadcast(mSender, broadcast));
        }

        public static void Send(this Player mSender, NeutronWriter writer, Handle handle)
        {
            SocketHelper.Redirect(mSender, handle.protocol, handle.sendTo, writer.ToArray(), MatchmakingHelper.Broadcast(mSender, handle.broadcast));
        }

        public static void Send(this Player mSender, NeutronWriter writer)
        {
            SocketHelper.Redirect(mSender, Protocol.Tcp, SendTo.Me, writer.ToArray(), MatchmakingHelper.Broadcast(mSender, Broadcast.Me));
        }

        public static bool IsInChannel(this Player _player)
        {
            return _player.CurrentChannel > -1;
        }

        public static bool IsInRoom(this Player _player)
        {
            return _player.CurrentRoom > -1;
        }
    }
}
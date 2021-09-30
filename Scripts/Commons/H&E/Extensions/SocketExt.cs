using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Packets;

namespace NeutronNetwork.Extensions
{
    public static class SocketExt
    {
        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronStream.IWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            MatchmakingHelper.Internal.Redirect(owner, sender, protocol, targetTo, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, tunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronStream.IWriter writer, HandlerOptions handler)
        {
            MatchmakingHelper.Internal.Redirect(owner, sender, handler.Protocol, handler.TargetTo, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronStream.IWriter writer)
        {
            MatchmakingHelper.Internal.Redirect(owner, sender, Protocol.Tcp, TargetTo.Me, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, TunnelingTo.Me));
        }

        public static void Write(this NeutronPlayer owner, NeutronStream.IWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            MatchmakingHelper.Internal.Redirect(owner, owner, protocol, targetTo, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, tunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronStream.IWriter writer, HandlerOptions handler)
        {
            MatchmakingHelper.Internal.Redirect(owner, owner, handler.Protocol, handler.TargetTo, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronStream.IWriter writer)
        {
            MatchmakingHelper.Internal.Redirect(owner, owner, Protocol.Tcp, TargetTo.Me, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, TunnelingTo.Me));
        }

        public static void Write(this NeutronPlayer owner, byte[] buffer)
        {
            MatchmakingHelper.Internal.Redirect(owner, owner, Protocol.Tcp, TargetTo.Me, buffer, MatchmakingHelper.Internal.Tunneling(owner, TunnelingTo.Me));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, byte[] buffer)
        {
            MatchmakingHelper.Internal.Redirect(owner, sender, Protocol.Tcp, TargetTo.Me, buffer, MatchmakingHelper.Internal.Tunneling(owner, TunnelingTo.Me));
        }

        //public static void Write(this NeutronPacket packet, NeutronWriter writer, TargetTo targetTo)
        //{
        //    MatchmakingHelper.Internal.Redirect(packet, writer.ToArray(), targetTo, MatchmakingHelper.Tunneling(packet.Owner, TunnelingTo.Me));
        //}
    }
}
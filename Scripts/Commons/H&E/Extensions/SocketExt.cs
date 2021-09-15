using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;

namespace NeutronNetwork.Extensions
{
    public static class SocketExt
    {
        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, sender, protocol, targetTo, packet, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, tunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronWriter writer, HandlerOptions handler, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, sender, handler.Protocol, handler.TargetTo, packet, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronWriter writer, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, sender, Protocol.Tcp, TargetTo.Me, packet, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, TunnelingTo.Me));
        }

        public static void Write(this NeutronPlayer owner, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, owner, protocol, targetTo, packet, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, tunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronWriter writer, HandlerOptions handler, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, owner, handler.Protocol, handler.TargetTo, packet, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronWriter writer, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, owner, Protocol.Tcp, TargetTo.Me, packet, writer.ToArray(), MatchmakingHelper.Internal.Tunneling(owner, TunnelingTo.Me));
        }

        //public static void Write(this NeutronPacket packet, NeutronWriter writer, TargetTo targetTo)
        //{
        //    SocketHelper.Redirect(packet, writer.ToArray(), targetTo, MatchmakingHelper.Tunneling(packet.Owner, TunnelingTo.Me));
        //}
    }
}
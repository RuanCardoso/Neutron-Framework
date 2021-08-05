using NeutronNetwork.Helpers;
using NeutronNetwork.Server.Internal;

namespace NeutronNetwork.Extensions
{
    public static class SocketExt
    {
        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, sender, protocol, targetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(owner, tunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronWriter writer, HandlerOptions handler, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, sender, handler.Protocol, handler.TargetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(owner, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronPlayer sender, NeutronWriter writer, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, sender, Protocol.Tcp, TargetTo.Me, packet, writer.ToArray(), MatchmakingHelper.Tunneling(owner, TunnelingTo.Me));
        }

        public static void Write(this NeutronPlayer owner, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, owner, protocol, targetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(owner, tunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronWriter writer, HandlerOptions handler, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, owner, handler.Protocol, handler.TargetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(owner, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer owner, NeutronWriter writer, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(owner, owner, Protocol.Tcp, TargetTo.Me, packet, writer.ToArray(), MatchmakingHelper.Tunneling(owner, TunnelingTo.Me));
        }
    }
}
using NeutronNetwork.Helpers;
using NeutronNetwork.Server.Internal;

namespace NeutronNetwork.Extensions
{
    public static class SocketExt
    {
        public static void Write(this NeutronPlayer sender, NeutronPlayer dataSender, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(sender, dataSender, protocol, targetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(sender, tunnelingTo));
        }

        public static void Write(this NeutronPlayer sender, NeutronPlayer dataSender, NeutronWriter writer, NeutronDefaultHandlerOptions handler, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(sender, dataSender, handler.Protocol, handler.TargetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(sender, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer sender, NeutronPlayer dataSender, NeutronWriter writer, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(sender, dataSender, Protocol.Tcp, TargetTo.Me, packet, writer.ToArray(), MatchmakingHelper.Tunneling(sender, TunnelingTo.Me));
        }

        public static void Write(this NeutronPlayer sender, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(sender, sender, protocol, targetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(sender, tunnelingTo));
        }

        public static void Write(this NeutronPlayer sender, NeutronWriter writer, NeutronDefaultHandlerOptions handler, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(sender, sender, handler.Protocol, handler.TargetTo, packet, writer.ToArray(), MatchmakingHelper.Tunneling(sender, handler.TunnelingTo));
        }

        public static void Write(this NeutronPlayer sender, NeutronWriter writer, Packet packet = Packet.Empty)
        {
            SocketHelper.Redirect(sender, sender, Protocol.Tcp, TargetTo.Me, packet, writer.ToArray(), MatchmakingHelper.Tunneling(sender, TunnelingTo.Me));
        }
    }
}
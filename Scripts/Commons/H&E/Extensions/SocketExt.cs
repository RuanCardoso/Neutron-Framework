using NeutronNetwork.Helpers;
using NeutronNetwork.Server.Internal;

namespace NeutronNetwork.Extensions
{
    public static class SocketExt
    {
        public static void Send(this NeutronPlayer player, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            SocketHelper.Redirect(player, protocol, targetTo, writer.ToArray(), MatchmakingHelper.Tunneling(player, tunnelingTo));
        }

        public static void Send(this NeutronPlayer player, NeutronWriter writer, NeutronDefaultHandlerOptions handler)
        {
            SocketHelper.Redirect(player, handler.Protocol, handler.TargetTo, writer.ToArray(), MatchmakingHelper.Tunneling(player, handler.TunnelingTo));
        }

        public static void Send(this NeutronPlayer player, NeutronWriter writer)
        {
            SocketHelper.Redirect(player, Protocol.Tcp, TargetTo.Me, writer.ToArray(), MatchmakingHelper.Tunneling(player, TunnelingTo.Me));
        }
    }
}
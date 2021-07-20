using NeutronNetwork.Helpers;
using NeutronNetwork.Server.Internal;

namespace NeutronNetwork.Extensions
{
    public static class MatchmakingExt
    {
        public static bool IsInChannel(this NeutronPlayer player)
        {
            return player.CurrentChannel > -1;
        }

        public static bool IsInRoom(this NeutronPlayer player)
        {
            return player.CurrentRoom > -1;
        }
    }
}
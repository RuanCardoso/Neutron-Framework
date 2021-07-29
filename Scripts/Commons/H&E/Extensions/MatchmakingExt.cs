namespace NeutronNetwork.Extensions
{
    public static class MatchmakingExt
    {
        public static bool IsInChannel(this NeutronPlayer player)
        {
            return player.Channel != null;
        }

        public static bool IsInRoom(this NeutronPlayer player)
        {
            return player.Room != null;
        }

        public static bool IsInMatchmaking(this NeutronPlayer player)
        {
            return player.Matchmaking != null;
        }
    }
}
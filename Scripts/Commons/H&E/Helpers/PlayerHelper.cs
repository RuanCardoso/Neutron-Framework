using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;

namespace NeutronNetwork.Helpers
{
    public static class PlayerHelper
    {
        public static void Disconnect(NeutronPlayer player, string reason)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Disconnection);
                writer.Write(player.ID);
                writer.Write(reason);
                player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerDisconnected);
            }
        }

        public static bool IsMine(NeutronPlayer player, int playerId)
        {
            return player.ID == playerId;
        }

        public static bool GetAvailableID(out int id)
        {
            if (Neutron.Server._pooledIds.Count > 0)
            {
                if (!Neutron.Server._pooledIds.TryDequeue(out id))
                    id = 0;
            }
            else
                id = 0;
            return id > NeutronConstantsSettings.GENERATE_PLAYER_ID;
        }
    }
}
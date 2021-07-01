using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Components;

namespace NeutronNetwork.Helpers
{
    public static class PlayerHelper
    {
        public static void Disconnect(Player nPlayer, string reason)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Disconnection);
                writer.Write(nPlayer.ID);
                writer.Write(reason);
                nPlayer.Send(writer, NeutronConfig.Settings.HandleSettings.OnPlayerDisconnected);
            }
        }

        public static void Message(Player nSocket, SystemPacket packet, string message)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Fail);
                writer.WritePacket(packet);
                writer.Write(message);
                nSocket.Send(writer);
            }
        }

        public static string GetNickname(int ID)
        {
            return $"Player#{ID}";
        }

        public static bool IsMine(Player nSender, int networkID)
        {
            return nSender.ID == networkID;
        }

        public static bool GetAvailableID(out int ID)
        {
            #region Provider
            if (Neutron.Server.generatedIds.Count > 0)
            {
                if (!Neutron.Server.generatedIds.TryDequeue(out ID))
                    ID = 0;
            }
            else ID = 0;
            #endregion

            #region Return
            return ID > NeutronConstants.GENERATE_PLAYER_ID;
            #endregion
        }
    }
}
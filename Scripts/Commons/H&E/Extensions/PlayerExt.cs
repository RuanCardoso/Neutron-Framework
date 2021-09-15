using NeutronNetwork.Internal.Packets;

namespace NeutronNetwork.Extensions
{
    public static class PlayerExt
    {
        public static void Message(this NeutronPlayer player, Packet packet, string message)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.Fail);
                writer.WritePacket((byte)packet);
                writer.Write(message);
                player.Write(writer);
            }
        }
    }
}
namespace NeutronNetwork.Extensions
{
    public static class PlayerExt
    {
        public static void Message(this NeutronPlayer player, Packet packet, string message)
        {
#if !UNITY_EDITOR
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Fail);
                writer.WritePacket(packet);
                writer.Write(message);
                player.Write(writer);
            }
#else
            LogHelper.Error($"[{packet}] -> | ERROR | {message}");
#endif
        }
    }
}
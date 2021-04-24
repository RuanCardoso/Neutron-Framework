using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;

public static class PlayerHelper
{
    public static void Disconnect(Player nPlayer, string reason)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.DisconnectedByReason);
            writer.Write(reason);
            nPlayer.Send(writer);
        }
    }

    public static void Message(Player nSocket, Packet packet, string message)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.Fail);
            writer.WritePacket(packet);
            writer.Write(message);
            nSocket.Send(writer);
        }
    }

    public static bool GetAvailableID(out int ID)
    {
        if (Neutron.Server.generatedIds.SafeCount > 0)
            ID = Neutron.Server.generatedIds.SafeDequeue();
        else ID = 0;
        return ID > Neutron.GENERATE_PLAYER_ID;
    }
}
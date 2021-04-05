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
}
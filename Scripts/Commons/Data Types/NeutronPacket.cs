using NeutronNetwork;
using System;

[Serializable]
public class NeutronPacket
{
    public byte[] Buffer { get; }
    public NeutronPlayer Owner { get; }
    public NeutronPlayer Sender { get; }
    public Protocol Protocol { get; }
    public Packet Packet { get; } = Packet.Empty;
    public bool IsServerSide { get; set; }

    public NeutronPacket(byte[] buffer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol)
    {
        Buffer = buffer;
        Owner = owner;
        Sender = sender;
        Protocol = protocol;
    }

    public NeutronPacket(NeutronWriter writer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol)
    {
        Buffer = writer.ToArray();
        Owner = owner;
        Sender = sender;
        Protocol = protocol;
    }

    public NeutronPacket(byte[] buffer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol, Packet packet)
    {
        Buffer = buffer;
        Owner = owner;
        Sender = sender;
        Protocol = protocol;
        Packet = packet;
    }
}